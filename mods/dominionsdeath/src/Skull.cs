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
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using dmplayer.src;

namespace dmskull.src
{ 
    static class Key
    {
        // Local attributes
        // Skull of player uid
        public static string attrUid = "dmUid";
        // Skull creation time
        public static string attrCreated = "dmCreated";
        public static string eventRespawn = "dmRespawn";
        public static string behaviorDmPlayer = "dmDeathBehavior";
    }

    class BERespawnSkull : BlockEntity
    {
        // Owner of the skull
        public string uid;
        public double created;

        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(byItemStack);

            if (byItemStack == null) return;

            uid = byItemStack.Attributes.GetString(Key.attrUid, "");
            created = byItemStack.Attributes.GetDouble(Key.attrCreated, 0);
        }

        public override void FromTreeAtributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAtributes(tree, worldAccessForResolve);
            uid = tree.GetString(Key.attrUid, "");
            created = tree.GetDouble(Key.attrCreated, 0);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetString(Key.attrUid, uid);
            tree.SetDouble(Key.attrCreated, created);
        }

    }

    class RespawnSkull : HorizontalRotation
    {
        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            BERespawnSkull entity = (BERespawnSkull)api.World.BlockAccessor.GetBlockEntity(pos);
            if (entity == null) return null;

            ItemStack[] stacks = base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier);

            if (stacks.Length < 1) return null;
            stacks[0].Attributes.SetString(Key.attrUid, entity.uid);
            stacks[0].Attributes.SetDouble(Key.attrCreated, entity.created);

            return stacks;
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) { return true; }

        public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (api.Side.IsClient()) return;

            ICoreServerAPI sApi = (ICoreServerAPI)api;

            // Has respawn skull block entity
            BERespawnSkull respawnSkull = (BERespawnSkull)api.World.BlockAccessor.GetBlockEntity(blockSel.Position);
            if (respawnSkull == null) return;

            string uid = respawnSkull.uid;

            IServerPlayer sPlayer = (IServerPlayer)sApi.World.PlayerByUid(uid);
            if (sPlayer == null) return;

            DeathBehavior behavior = sPlayer.Entity.GetBehavior<DeathBehavior>();
            if (behavior == null) return;

            double lastSpawn = behavior.LastSpawn;
            double spawnTime = behavior.SpawnTime;

            // If created earlier than the last spawn, it's expired, if less than total hours, expired
            if (respawnSkull.created < lastSpawn ||
                spawnTime < sApi.World.Calendar.TotalHours) return;

            // If has correct item, consume it
            if (byPlayer.InventoryManager.ActiveHotbarSlot == null ||
                byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack == null) return;

            ItemStack stack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
            Item stackItem = stack.Item;

            string offeringCode = "game:fat"; // Some other ritual later on?

            if (stackItem == null ||
                stackItem.Code.ToString() != offeringCode ||
                byPlayer.InventoryManager.ActiveHotbarSlot.TryPutInto(world, new DummySlot()) == 0) return;

            behavior.SpawnTime = 0; // Reset spawn time to expired, behavior will handle
            behavior.Relocate = false; // Don't relocate

            // Set spawn on skull
            sPlayer.SetSpawnPosition(new PlayerSpawnPos(blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z));
            sApi.Server.LogEvent("Respawned dead player using skull ritual.");

            // Remove skull, add particles at some point
            sApi.World.BlockAccessor.BreakBlock(blockSel.Position, null, 0);

            base.OnBlockInteractStop(secondsUsed, world, byPlayer, blockSel);
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            dsc.AppendLine("<font color=\"DeepSkyBlue\">Human Skull</font>");
            dsc.AppendLine("In ancient rituals, fat would be offered over the bones of the dead to appease spirits and give the deceased a safe passing.");
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            return new WorldInteraction[]
            {
                new WorldInteraction()
                {
                    ActionLangCode = "dmskull:block-makeoffering",
                    MouseButton = EnumMouseButton.Right,
                    Itemstacks = new ItemStack[]
                    {
                        new ItemStack(api.World.GetItem(new AssetLocation("fat")), 1)
                    }
                }
            };
        }
    }

    class HorizontalRotation : Block
    {
        static readonly string defaultOrientation = "south";

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            ItemStack stack = new ItemStack(api.World.GetBlock(CodeWithParts(defaultOrientation)), (int)(1 * dropQuantityMultiplier));

            return new ItemStack[]
            {
                stack
            };
        }

        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
        {
            BlockFacing orientation = SuggestedHVOrientation(byPlayer, blockSel)[0];
            Block block = world.GetBlock(CodeWithParts(orientation.Code));

            world.BlockAccessor.SetBlock(block.BlockId, blockSel.Position, byItemStack);

            return true;
        }
    }

    class RegisterRespawnSkull : ModSystem
    {
        ICoreServerAPI sApi;

        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.World.Logger.Event("Loading DMSKULL");
            api.RegisterBlockClass("dmHorizontalOrientation", typeof(HorizontalRotation));
            api.RegisterBlockClass("dmRespawnSkull", typeof(RespawnSkull));
            api.RegisterBlockEntityClass("dmBERespawnSkull", typeof(BERespawnSkull));

            if (api.Side.IsClient()) return;
            sApi = (ICoreServerAPI)api;
            sApi.Event.PlayerDeath += PlaceOnDeath;
        }

        void PlaceOnDeath(IServerPlayer byPlayer, DamageSource dmgSource)
        {
            DeathBehavior behavior = byPlayer.Entity.GetBehavior<DeathBehavior>();
            if (behavior == null) return;

            // Verify just died
            if (!behavior.Died) return;

            // Get block to drop on death
            string code = "dmskull:skull-south";
            Block block = sApi.World.BlockAccessor.GetBlock(new AssetLocation(code));
            if (block == null) return;

            // Inject id of player who died
            ItemStack stack = new ItemStack();
            stack.Attributes.SetString(Key.attrUid, byPlayer.PlayerUID);
            stack.Attributes.SetDouble(Key.attrCreated, sApi.World.Calendar.TotalHours);

            sApi.World.BlockAccessor.SetBlock(block.Id, byPlayer.Entity.Pos.AsBlockPos, stack);
        }
    }
}
