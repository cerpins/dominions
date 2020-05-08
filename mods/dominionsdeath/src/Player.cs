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
        private static double secondsInHr = 3600;

        // Respawn time after death
        private static double hoursUntilRespawn = 1;

        // How often we try respawn
        private static int tryRespawnMs = 1000;

        // Double timeout on death
        private static string attrSpawnTime = "dmSpawnTime";

        // Bool toggle whether to not set new spawn location on next respawn
        private static string attrRelocate = "dmRelocate";

        // Bool true if converting from alive to spectator
        private static string attrDied = "dmDied";

        // Double time last respawn
        private static string attrLastSpawn = "dmLastSpawn";

        // -1 if alive, 0 so your first spawn consumes a timer and spawns you normally
        private static double aliveSpawnTime = -1;

        private long tryRespawnListener;
        private ICoreServerAPI sApi;
        private EntityPlayer entityPlayer;

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

        public DeathBehavior(Entity entity) : base(entity)
        {
        }

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

        public override void OnGameTick(float deltaTime)
        {
            entityPlayer.CameraPos.Add(0, 50, 0);
        }

        public override void OnEntityReceiveDamage(DamageSource damageSource, float damage)
        {
            if (damageSource.Source != EnumDamageSource.Revive) return; // Only on respawn
            Died = false; // Death handled

            IServerPlayer player = (IServerPlayer)entityPlayer.Player;

            if (SpawnTime > sApi.World.Calendar.TotalHours)
            {
                // Timer not expired, spectate
                player.WorldData.CurrentGameMode = EnumGameMode.Spectator;
                player.WorldData.MoveSpeedMultiplier = 0.5f;
            }
            else
            {
                LastSpawn = sApi.World.Calendar.TotalHours;
                SpawnTime = aliveSpawnTime;

                player.WorldData.CurrentGameMode = EnumGameMode.Survival;
                player.WorldData.MoveSpeedMultiplier = 1f;
            }

            player.BroadcastPlayerData();

            base.OnEntityReceiveDamage(damageSource, damage);
        }

        public override void OnEntityDeath(DamageSource damageSourceForDeath)
        {
            IServerPlayer player = (IServerPlayer)entityPlayer.Player;

            double totalHours = sApi.World.Calendar.TotalHours;
            BlockPos spawnPos = entity.ServerPos.AsBlockPos;

            if (SpawnTime == aliveSpawnTime) // Died
            {
                double time = Util.ToGameTime(sApi.World.Calendar, hoursUntilRespawn);
                SpawnTime = totalHours + time;
                Died = true;
            }
            else if (SpawnTime < totalHours) // Will respawn
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

        private void TryRespawn(float dt)
        {
            if (entity.Alive && SpawnTime != aliveSpawnTime && SpawnTime < sApi.World.Calendar.TotalHours)
            {
                entity.Die();
            }
        }
    }

    internal class Register : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.World.Logger.Event("Loading DMPLAYER");
            api.RegisterEntityBehaviorClass("dmDeathBehavior", typeof(DeathBehavior));

            if (api.Side.IsClient()) return;
            ICoreServerAPI sApi = (ICoreServerAPI)api;

            sApi.RegisterCommand("respawn", "Remaining time to spawn", "", cmdRespawn);
        }

        private void cmdRespawn(IServerPlayer player, int groupId, CmdArgs args)
        {
            DeathBehavior behavior = player.Entity.GetBehavior<DeathBehavior>();

            string msg = "Failed to get respawn time";

            if (behavior != null)
            {
                IGameCalendar calendar = player.Entity.Api.World.Calendar;
                msg = "Real life hours until respawn: " + Math.Round(Util.FromGameTime(calendar, behavior.SpawnTime - calendar.TotalHours), 2);
            }

            player.SendMessage(GlobalConstants.AllChatGroups, msg, EnumChatType.Notification);
        }
    }
}