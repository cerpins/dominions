using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;

namespace dmutils.src
{
    public class ItemAxeLowerDrops : ItemAxe
    {
        public static SimpleParticleProperties dustParticles = new SimpleParticleProperties()
        {
            MinPos = new Vec3d(),
            AddPos = new Vec3d(),
            MinQuantity = 0,
            AddQuantity = 3,
            Color = ColorUtil.ToRgba(100, 200, 200, 200),
            GravityEffect = 1f,
            WithTerrainCollision = true,
            ParticleModel = EnumParticleModel.Quad,
            LifeLength = 0.5f,
            MinVelocity = new Vec3f(-1, 2, -1),
            AddVelocity = new Vec3f(2, 0, 2),
            MinSize = 0.07f,
            MaxSize = 0.1f,
            WindAffected = true
        };
        static ItemAxeLowerDrops()
        {

            dustParticles.ParticleModel = EnumParticleModel.Quad;
            dustParticles.AddPos.Set(1, 1, 1);
            dustParticles.MinQuantity = 2;
            dustParticles.AddQuantity = 12;
            dustParticles.LifeLength = 4f;
            dustParticles.MinSize = 0.2f;
            dustParticles.MaxSize = 0.5f;
            dustParticles.MinVelocity.Set(-0.4f, -0.4f, -0.4f);
            dustParticles.AddVelocity.Set(0.8f, 1.2f, 0.8f);
            dustParticles.DieOnRainHeightmap = false;
            dustParticles.WindAffectednes = 0.5f;
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {

            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        }

        public override bool OnBlockBrokenWith(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel)
        {
            // Copy of Tyron's code with an edit to the amount of logs dropped

            IPlayer byPlayer = null;
            if (byEntity is EntityPlayer) byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);

            ITreeAttribute tempAttr = itemslot.Itemstack.TempAttributes;
            //tempAttr.SetInt("breakCounter", 0);


            WeatherSystemBase wBase = api.ModLoader.GetModSystem<WeatherSystemBase>();
            double windspeed = (wBase == null) ? 0 : wBase.GetWindSpeed(byEntity.LocalPos.XYZ);

            string treeType;
            Stack<BlockPos> foundPositions = FindTree(world, blockSel.Position, out treeType);

            if (treeType == "baobab")
            {
                // Dont loop logs for baobabs
                world.BlockAccessor.BreakBlock(blockSel.Position, byPlayer, 1f);
                return true;
            }

            Block leavesBranchyBlock = world.GetBlock(new AssetLocation("leavesbranchy-grown-" + treeType));
            Block leavesBlock = world.GetBlock(new AssetLocation("leaves-grown-" + treeType));

            if (foundPositions.Count == 0)
            {
                return base.OnBlockBrokenWith(world, byEntity, itemslot, blockSel);
            }

            bool damageable = DamagedBy != null && DamagedBy.Contains(EnumItemDamageSource.BlockBreaking);

            float leavesMul = 1;
            float leavesBranchyMul = 0.8f;
            int blocksbroken = 0;

            // Drop per logs
            int dropEveryNthLog = 3;
            int dropEveryNthLogBig = 6; // Less drops on giant tree

            // Big tree margin
            int bigTreeLogs = 24;

            // Check if big tree
            bool isBigTree = (foundPositions.Count >= bigTreeLogs);

            // For variety
            float dropMul = 0.9f;

            while (foundPositions.Count > 0)
            {
                BlockPos pos = foundPositions.Pop();
                blocksbroken++;

                Block block = world.BlockAccessor.GetBlock(pos);

                bool isLog = block.Code.Path.StartsWith("beehive-inlog-" + treeType) || block.Code.Path.StartsWith("log-resinharvested-" + treeType) || block.Code.Path.StartsWith("log-resin-" + treeType) || block.Code.Path.StartsWith("log-grown-" + treeType) || block.Code.Path.StartsWith("bamboo-grown-brown-segment") || block.Code.Path.StartsWith("bamboo-grown-green-segment");
                bool isBranchy = block == leavesBranchyBlock;
                bool isLeaves = block == leavesBlock || block.Code.Path == "bambooleaves-grown";

                // Calc drops for small and big trees
                float doDrop = (foundPositions.Count < dropEveryNthLog) ? 
                    1 : // Always drop block per log if it's smaller than the expected tree size
                    ((foundPositions.Count % (isBigTree ? dropEveryNthLogBig : dropEveryNthLog)) == 0) ? dropMul : 0;

                world.BlockAccessor.BreakBlock(pos, byPlayer, isLeaves ? leavesMul : (isBranchy ? leavesBranchyMul : 1 * doDrop));

                if (world.Side == EnumAppSide.Client)
                {

                    dustParticles.Color = block.GetRandomColor(world.Api as ICoreClientAPI, pos, BlockFacing.UP);
                    dustParticles.Color |= 255 << 24;
                    dustParticles.MinPos.Set(pos.X, pos.Y, pos.Z);

                    if (block.BlockMaterial == EnumBlockMaterial.Leaves)
                    {
                        dustParticles.GravityEffect = (float)world.Rand.NextDouble() * 0.1f + 0.01f;
                        dustParticles.ParticleModel = EnumParticleModel.Quad;
                        dustParticles.MinVelocity.Set(-0.4f + 4 * (float)windspeed, -0.4f, -0.4f);
                        dustParticles.AddVelocity.Set(0.8f + 4 * (float)windspeed, 1.2f, 0.8f);

                    }
                    else
                    {
                        dustParticles.GravityEffect = 0.8f;
                        dustParticles.ParticleModel = EnumParticleModel.Cube;
                        dustParticles.MinVelocity.Set(-0.4f + (float)windspeed, -0.4f, -0.4f);
                        dustParticles.AddVelocity.Set(0.8f + (float)windspeed, 1.2f, 0.8f);
                    }


                    world.SpawnParticles(dustParticles);
                }


                if (damageable && isLog)
                {
                    DamageItem(world, byEntity, itemslot);
                }

                if (itemslot.Itemstack == null) return true;

                if (isLeaves && leavesMul > 0.03f) leavesMul *= 0.85f;
                if (isBranchy && leavesBranchyMul > 0.015f) leavesBranchyMul *= 0.6f;
            }

            if (blocksbroken > 35)
            {
                Vec3d pos = blockSel.Position.ToVec3d().Add(0.5, 0.5, 0.5);
                api.World.PlaySoundAt(new AssetLocation("sounds/effect/treefell"), pos.X, pos.Y, pos.Z, byPlayer, false, 32, GameMath.Clamp(blocksbroken / 100f, 0.25f, 1));
            }


            //`byPlayer.Entity.GetBehavior<EntityBehaviorHunger>()?.ConsumeSaturation(GameMath.Sqrt(foundPositions.Count));


            return true;

        }
    }
}
