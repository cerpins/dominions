using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;


namespace dmplayer.src
{
    public class DeathBehavior : EntityBehavior
    {
        // utility constants
        public double secondsInHr = 3600;

        // Respawn time after death 
        public double hoursUntilRespawn = 24;
        // How often we try respawn
        int tryRespawnMs = 1000;

        // Double timeout on death
        static string attrSpawnTime = "dmSpawnTime";
        // Bool toggle whether to not set new spawn location on next respawn
        static string attrRelocate = "dmRelocate";
        // Bool true if converting from alive to spectator
        static string attrDied = "dmDied";
        // Double time last respawn
        static string attrLastSpawn = "dmLastSpawn";

        // -1 if alive, 0 so your first spawn consumes a timer and spawns you normally
        static double aliveSpawnTime = -1;

        long tryRespawnListener;
        ICoreServerAPI sApi;
        EntityPlayer entityPlayer;

        public double SpawnTime
        {
            get
            {
                if (entity.Attributes.HasAttribute(attrSpawnTime)) return entity.Attributes.GetDouble(attrSpawnTime);
                entity.Attributes.SetDouble(attrSpawnTime, aliveSpawnTime);
                return entity.Attributes.GetDouble(attrSpawnTime);
            }
            set { entity.Attributes.SetDouble(attrSpawnTime, value); }
        }
        public bool Relocate
        {
            get
            {
                if (entity.Attributes.HasAttribute(attrRelocate)) return entity.Attributes.GetBool(attrRelocate);
                entity.Attributes.SetBool(attrRelocate, true);
                return entity.Attributes.GetBool(attrRelocate);
            }
            set { entity.Attributes.SetBool(attrRelocate, value); }
        }
        public bool Died
        {
            get
            {
                if (entity.Attributes.HasAttribute(attrDied)) return entity.Attributes.GetBool(attrDied);
                entity.Attributes.SetBool(attrDied, false);
                return entity.Attributes.GetBool(attrDied);
            }
            set { entity.Attributes.SetBool(attrDied, value); }
        }
        public double LastSpawn
        {
            get
            {
                if (entity.Attributes.HasAttribute(attrLastSpawn)) return entity.Attributes.GetDouble(attrLastSpawn);
                entity.Attributes.SetDouble(attrLastSpawn, 0);
                return entity.Attributes.GetDouble(attrLastSpawn);
            }
            set { entity.Attributes.SetDouble(attrLastSpawn, value); }
        }
        public DeathBehavior(Entity entity) : base(entity) { }

        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            base.Initialize(properties, attributes);

            if (entity.Api.Side.IsClient() || !(entity is EntityPlayer)) throw new Exception("Misuse of server side, player only behavior.");
            sApi = (ICoreServerAPI)entity.Api;
            entityPlayer = (EntityPlayer)entity;
        }

        public override void OnEntitySpawn()
        {
            base.OnEntitySpawn();
            tryRespawnListener = sApi.World.RegisterGameTickListener(TryRespawn, tryRespawnMs);
        }

        public override void OnEntityDespawn(EntityDespawnReason despawn)
        {
            base.OnEntityDespawn(despawn);
            sApi.World.UnregisterGameTickListener(tryRespawnListener);
        }

        public override void OnEntityReceiveDamage(DamageSource damageSource, float damage)
        {
            // For some reason, player is still flying when this executes on server

            if (damageSource.Source != EnumDamageSource.Revive) return; // Only on respawn
            Died = false; // Death handled

            IServerPlayer player = (IServerPlayer)entityPlayer.Player;
            double totalTime = GetTimeInSeconds();

            if (SpawnTime > totalTime)
            {
                // Timer not expired, spectate
                sApi.InjectConsole("/gamemode " + player.PlayerName + " 3");
                player.WorldData.MoveSpeedMultiplier = 0.5f;
            }
            else
            {
                LastSpawn = totalTime;
                SpawnTime = aliveSpawnTime;

                sApi.InjectConsole("/gamemode " + player.PlayerName + " 1");
                player.WorldData.MoveSpeedMultiplier = 1f;
            }

            player.BroadcastPlayerData();

            base.OnEntityReceiveDamage(damageSource, damage);
        }

        public int GetTimeInSeconds()
        {
            int unixTime = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            return unixTime;
        }

        public override void OnEntityDeath(DamageSource damageSourceForDeath)
        {
            IServerPlayer player = (IServerPlayer)entityPlayer.Player;
            BlockPos spawnPos = entity.ServerPos.AsBlockPos;

            double totalTime = GetTimeInSeconds();

            if (SpawnTime == aliveSpawnTime) // Died
            {
                double time = hoursUntilRespawn * secondsInHr;
                SpawnTime = totalTime + time;

                Died = true;
            }
            else if (SpawnTime < totalTime) // Will respawn
            {
                spawnPos = sApi.World.DefaultSpawnPosition.AsBlockPos;
            }
            else { /* Died while spectating */ }

            if (Relocate)
            {
                PlayerSpawnPos pos = new PlayerSpawnPos(spawnPos.X, spawnPos.Y, spawnPos.Z);
                player.SetSpawnPosition(pos);
            }

            Relocate = true; // Toggle back
        }

        public override string PropertyName()
        {
            return "dmDeathBehavior";
        }

        public void DoRespawn()
        {
            if (entity.Alive && SpawnTime != aliveSpawnTime)
            {
                SpawnTime = GetTimeInSeconds() - 1;
                entity.Die();
            }
        }

        void TryRespawn(float dt)
        {
            double totalTime = GetTimeInSeconds();
            if (entity.Alive && SpawnTime != aliveSpawnTime && SpawnTime < totalTime)
            {
                entity.Die();
            }
        }
    }

    class Register : ModSystem
    {
        ICoreAPI api;

        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            this.api = api;

            api.World.Logger.Event("Loading DMPLAYER");
            api.RegisterEntityBehaviorClass("dmDeathBehavior", typeof(DeathBehavior));

            if (api.Side.IsClient()) return;
            ICoreServerAPI sApi = (ICoreServerAPI)api;

            sApi.RegisterCommand("respawn", "Remaining time to spawn", "", CmdRespawn);
            sApi.RegisterCommand("spawnsingle", "Spawn a player", "", CmdSpawnSingle, Privilege.gamemode);
            sApi.RegisterCommand("spawnall", "Spawn all players", "", CmdSpawnAll, Privilege.gamemode);
        }

        private void CmdSpawnAll(IServerPlayer player, int groupId, CmdArgs args)
        {
            string msg = "Spawned all players";

            if (!(api is ICoreServerAPI)) return;
            ICoreServerAPI sapi = (ICoreServerAPI)api;

            IPlayer[] players = sapi.World.AllPlayers;
            foreach (IPlayer target in players)
            {
                IServerPlayer found = (IServerPlayer)target;
                DeathBehavior behavior = found.Entity.GetBehavior<DeathBehavior>();
                if (behavior == null) return;

                behavior.DoRespawn();
            }

            player.SendMessage(GlobalConstants.AllChatGroups, msg, EnumChatType.Notification);
        }

        void CmdSpawnSingle(IServerPlayer player, int groupId, CmdArgs args)
        {
            string msg = "Failed to find player";
            if (args.Length != 1)
            {
                msg = "Wrong amount of args passed";
                player.SendMessage(GlobalConstants.AllChatGroups, msg, EnumChatType.Notification);
                return;
            }

            if (!(api is ICoreServerAPI)) return;
            ICoreServerAPI sapi = (ICoreServerAPI)api;

            IPlayer[] players = sapi.World.AllPlayers;
            foreach (IPlayer target in players)
            {
                if (target.PlayerName != args[0]) continue;
                
                IServerPlayer found = (IServerPlayer)target;
                DeathBehavior behavior = found.Entity.GetBehavior<DeathBehavior>();
                if (behavior == null) return;

                behavior.DoRespawn();

                msg = "Respawned single player";
                player.SendMessage(GlobalConstants.AllChatGroups, msg, EnumChatType.Notification);
                return;
            }

            player.SendMessage(GlobalConstants.AllChatGroups, msg, EnumChatType.Notification);
        }

        void CmdRespawn(IServerPlayer player, int groupId, CmdArgs args)
        {
            DeathBehavior behavior = player.Entity.GetBehavior<DeathBehavior>();

            string msg = "Failed to get respawn time";

            if (behavior != null)
            {
                int totalTime = behavior.GetTimeInSeconds();
                double remainingInSec = behavior.SpawnTime - totalTime;
                double remainingInMin = Math.Round(remainingInSec / (behavior.secondsInHr / 60), 0);
                double remainingInHr = Math.Round(remainingInSec / behavior.secondsInHr, 0);

                msg = (remainingInSec < 0) ? 
                    "Alive or respawning." :
                    "Time untill respawn:\nIn hours: " + remainingInHr + "\nIn minutes: " + remainingInMin + "\nIn seconds: " + remainingInSec;
            }

            player.SendMessage(GlobalConstants.AllChatGroups, msg, EnumChatType.Notification);
        }
    }
}
