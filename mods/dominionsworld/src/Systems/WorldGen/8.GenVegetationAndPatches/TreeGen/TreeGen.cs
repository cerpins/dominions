using System;
using System.Collections.Generic;
using System.Threading;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.ServerMods.NoObf;

namespace dominions.Systems.TreeGen
{
    /* Known problems
     * No yHardOffset for branch of girth 2 or girth 0
    */

    // Handles generation of the tree based on the given config
    public class TreeGen : ITreeGenerator
    {
        // "Temporary Values" linked to currently generated tree
        IBlockAccessor api;
        float size;
        float vineGrowthChance; // 0..1
        float otherBlockChance; // 0..1

        static ThreadLocal<Random> rand = new ThreadLocal<Random>(() => new Random(Environment.TickCount));

        List<TreeGenBranch> branchesByDepth = new List<TreeGenBranch>();
        ThreadLocal<LCGRandom> lcgrandTL;

        // Tree config
        TreeGenConfig config;

        public TreeGen(TreeGenConfig config, int seed)
        {
            this.config = config;
            lcgrandTL = new ThreadLocal<LCGRandom>(() => new LCGRandom(seed));
        }

        public void GrowTree(IBlockAccessor api, BlockPos pos, float sizeModifier = 1f, float vineGrowthChance = 0, float otherBlockChance = 1f)
        {
            this.api = api;
            this.size = sizeModifier * config.sizeMultiplier;
            this.vineGrowthChance = vineGrowthChance;
            this.otherBlockChance = otherBlockChance; 

            pos.Up(config.yOffset);

            TreeGenTrunk[] trunks = config.trunks;

            branchesByDepth.Clear();
            branchesByDepth.Add(null); 
            branchesByDepth.AddRange(config.branches); 

            Random rnd = rand.Value;

            for (int i = 0; i < trunks.Length; i++)
            {
                TreeGenTrunk trunk = config.trunks[i];

                if (rnd.NextDouble() <= trunk.probability)
                {
                    branchesByDepth[0] = trunk;

                    GrowBranch(
                        rnd,
                        0, pos, trunk.dx, 0f, trunk.dz,
                        trunk.angleVert.nextFloat(1, rnd),
                        trunk.angleHori.nextFloat(1, rnd),
                        size * trunk.widthMultiplier
                    );
                }
            }
        }

        private void GrowBranch(Random rand, int depth, BlockPos pos, float dx, float dy, float dz, float angleVerStart, float angleHorStart, float curWidth)
        {
            if (depth > 30) { Console.WriteLine("TreeGen.growBranch() aborted, too many branches!"); return; }

            TreeGenBranch branch = branchesByDepth[Math.Min(depth, branchesByDepth.Count - 1)];

            float branchDieAt = branch.dieAt.nextFloat(1, rand);

            float branchQuantityStart = branch.branchQuantity.nextFloat(1, rand);
            float branchWidthMulitplierStart = branch.branchWidthMultiplier.nextFloat(1, rand);

            float totaliterations = curWidth / branch.widthloss;

            int iteration = 0;
            float lastbranchiteration = 0;
            // branch spacing based on percent of total iterations
            float branchiterationspacing = branch.branchSpacing.nextFloat(1, rand) * totaliterations;
            // branch start based on percent of total iterations
            float branchiterationstart = branch.branchStart.nextFloat(1, rand) * totaliterations;

            float sequencesPerIteration = 1f / (curWidth / branch.widthloss);

            
            float ddrag = 0, angleVer = 0, angleHor = 0;

            // we want to place around the trunk/branch => offset the coordinates when growing stuff from the base
            float trunkOffsetX = 0, trunkOffsetZ = 0, trunkOffsetY = 0;

            BlockPos currentPos;

            float branchQuantity, branchWidth;
            float sinAngleVer, cosAnglerHor, sinAngleHor;

            float currentSequence;

            LCGRandom lcgrand = lcgrandTL.Value;

            bool doHardOffset = (branch.yHardOffset != 0);

            while (curWidth > 0 && iteration++ < 5000)
            {
                curWidth -= branch.widthloss;
                
                currentSequence = sequencesPerIteration * (iteration - 1);

                if (curWidth <= branchDieAt) break;

                angleVer = branch.angleVertEvolve.nextFloat(angleVerStart, currentSequence);
                angleHor = branch.angleHoriEvolve.nextFloat(angleHorStart, currentSequence);

                sinAngleVer = GameMath.FastSin(angleVer);
                cosAnglerHor = GameMath.FastCos(angleHor);
                sinAngleHor = GameMath.FastSin(angleHor);

                trunkOffsetX = Math.Max(-0.5f, Math.Min(0.5f, 0.7f * sinAngleVer * cosAnglerHor));
                trunkOffsetY = Math.Max(-0.5f, Math.Min(0.5f, 0.7f * cosAnglerHor)) + 0.5f;
                trunkOffsetZ = Math.Max(-0.5f, Math.Min(0.5f, 0.7f * sinAngleVer * sinAngleHor));

                ddrag = branch.gravityDrag * GameMath.FastSqrt(dx * dx + dz * dz);

                dx += sinAngleVer * cosAnglerHor / Math.Max(1, Math.Abs(ddrag));
                dy += Math.Min(1, Math.Max(-1, GameMath.FastCos(angleVer) - ddrag));
                dz += sinAngleVer * sinAngleHor / Math.Max(1, Math.Abs(ddrag));

                int blockId = getBlockId(curWidth);
                if (blockId == 0) return;

                currentPos = pos.AddCopy(dx, dy, dz);

                PlaceResumeState state = getPlaceResumeState(currentPos, blockId);

                if (state == PlaceResumeState.CanPlace)
                {
                    if (branch.girth > 0)
                    {
                        // Circle fill algorithm
                        // Radius
                        int radius = (int)(branch.girth * branch.girthEvolve.nextFloat(1, iteration));
                        
                        if (doHardOffset)
                        {
                            for (int i = 0; i < Math.Abs(branch.yHardOffset); i++)
                            {
                                BlockFacing facing = (branch.yHardOffset < 0) ? BlockFacing.DOWN : BlockFacing.UP;
                                PlaceFillCircle(radius, blockId, currentPos.AddCopy(facing, i));
                            }

                            doHardOffset = false;
                        }

                        PlaceFillCircle(radius, blockId, currentPos.Copy());
                    }
                    else api.SetBlock(blockId, currentPos);

                    if (vineGrowthChance > 0 && rand.NextDouble() < vineGrowthChance && config.treeBlocks.vinesBlock != null)
                    {
                        BlockFacing facing = BlockFacing.HORIZONTALS[rand.Next(4)];

                        BlockPos vinePos = currentPos.AddCopy(facing);
                        float cnt = 1 + rand.Next(11) * (vineGrowthChance + 0.2f);

                        while (api.GetBlockId(vinePos) == 0 && cnt-- > 0)
                        {
                            Block block = config.treeBlocks.vinesBlock;

                            if (cnt <= 0 && config.treeBlocks.vinesEndBlock != null)
                            {
                                block = config.treeBlocks.vinesEndBlock;
                            }

                            block.TryPlaceBlockForWorldGen(api, vinePos, facing, lcgrand);
                            vinePos.Down();
                        }
                    }
                }
                else
                {
                    if (state == PlaceResumeState.Stop)
                    {
                        return;
                    }
                }

                // This is over-engineered and limiting
                //reldistance = GameMath.FastSqrt(dx * dx + dy * dy + dz * dz) / totaliterations;               

                
                if (iteration < branchiterationstart) continue;

                if (iteration > lastbranchiteration + branchiterationspacing)
                {
                    // Generating branches
                    lastbranchiteration = iteration;
                    branchiterationspacing = branch.branchSpacing.nextFloat(1, rand) * totaliterations;

                    if (branch.capBranch)
                    {
                        curWidth = branchDieAt;
                    }

                    if (branch.branchQuantityEvolve != null)
                    {
                        branchQuantity = branch.branchQuantityEvolve.nextFloat(branchQuantityStart, currentSequence);
                    } 
                    else
                    {
                        branchQuantity = branch.branchQuantity.nextFloat(1, rand);
                    }

                    float prevHorAngle = 0f;
                    float horAngle;
                    float minHorangleDist = Math.Min(GameMath.PI / 10, branch.branchHorizontalAngle.var / 5);
                    

                    bool first = true;

                    while (branchQuantity-- > 0)
                    {
                        if (branchQuantity < 1 && rand.NextDouble() < branchQuantity) break;

                        curWidth *= branch.branchWidthLossMul;

                        horAngle = branch.branchHorizontalAngle.nextFloat(1, rand);

                        int tries = 5;
                        while (!first && Math.Abs(horAngle - prevHorAngle) < minHorangleDist && tries-- > 0)
                        {
                            horAngle = branch.branchHorizontalAngle.nextFloat(1, rand);
                        }

                        if (branch.branchWidthMultiplierEvolve != null)
                        {
                            branchWidth = curWidth * branch.branchWidthMultiplierEvolve.nextFloat(branchWidthMulitplierStart, currentSequence);
                        } else
                        {
                            branchWidth = branch.branchWidthMultiplier.nextFloat(curWidth, rand);
                        }

                        GrowBranch(
                            rand,
                            depth + 1, 
                            pos, dx + trunkOffsetX, dy, dz + trunkOffsetZ, 
                            branch.branchVerticalAngle.nextFloat(1, rand), 
                            angleHor + branch.branchHorizontalAngle.nextFloat(1, rand), 
                            branchWidth
                        );

                        first = false;
                        prevHorAngle = horAngle;
                    }
                }
            }
        }

        public void PlaceFillCircle(int radius, int blockId, BlockPos startPos)
        {
            int rx = radius;
            int rz = 0;
            int cd = 0;

            // midline
            //for (int i = 0; i < (rx << 1); i++) api.SetBlock(blockId, startPos.AddCopy(-rx + i, 0, 0));

            while (rx > rz)
            {
                cd -= (--rx) - (++rz);
                if (cd < 0) cd += rx++;

                // Fixes some problems with radius 2
                int radiusFix = radius == 2 ? 1 : 0;

                // side A
                for (int i = 0; i < (rz << 1); i++) api.SetBlock(blockId, startPos.AddCopy(-rz + i, 0, -rx));
                for (int i = 0; i < (rx << 1); i++) api.SetBlock(blockId, startPos.AddCopy(-rx + i, 0, -rz));
                
                // side B
                for (int i = 0; i < (rx << 1); i++) api.SetBlock(blockId, startPos.AddCopy(-rx + i, 0, rz - 1));
                for (int i = 0; i < (rz << 1); i++) api.SetBlock(blockId, startPos.AddCopy(-rz + i, 0, rx - 1));

                if (radiusFix != 0)
                {
                    api.SetBlock(blockId, startPos.AddCopy(1, 0, 0));
                    api.SetBlock(blockId, startPos.AddCopy(1, 0, -1));
                    api.SetBlock(blockId, startPos.AddCopy(-2, 0, 0));
                    api.SetBlock(blockId, startPos.AddCopy(-2, 0, -1));
                    api.SetBlock(blockId, startPos.AddCopy(0, 0, 1));
                    api.SetBlock(blockId, startPos.AddCopy(-1, 0, 1));
                    api.SetBlock(blockId, startPos.AddCopy(0, 0, -2));
                    api.SetBlock(blockId, startPos.AddCopy(-1, 0, -2));
                }
            }
        }

        
        public int getBlockId(float width)
        {
            TreeBlocks blocks = config.treeBlocks;

            return
                width < 0.1f ? blocks.leavesBlockId : (
                    width < 0.3f ? (
                        rand.Value.NextDouble() < config.fruitChance ?
                            blocks.fruitLeavesBlockId :
                            blocks.leavesBranchyBlockId
                    ) : (blocks.otherLogBlockCode != null && rand.Value.NextDouble() < otherBlockChance * blocks.otherLogChance ? config.treeBlocks.otherLogBlockId : config.treeBlocks.logBlockId)
                )
            ;
        }

        PlaceResumeState getPlaceResumeState(BlockPos targetPos, int desiredblockId)
        {
            if (targetPos.X < 0 || targetPos.Y < 0 || targetPos.Z < 0 || targetPos.X >= api.MapSizeX || targetPos.Y >= api.MapSizeY || targetPos.Z >= api.MapSizeZ) return PlaceResumeState.Stop;

            // Should be like this but seems to work just fine anyway? o.O
            //int currentblockId = (api is IBulkBlockAccessor) ? ((IBulkBlockAccessor)api).GetStagedBlockId(targetPos) : api.GetBlockId(targetPos);
            int currentblockId = api.GetBlockId(targetPos);
            if (currentblockId == -1) return PlaceResumeState.CannotPlace;
            if (currentblockId == 0) return PlaceResumeState.CanPlace;

            Block currentBlock = api.GetBlock(currentblockId);
            Block desiredBock = api.GetBlock(desiredblockId);

            if (currentBlock.Replaceable < 6000 && !config.treeBlocks.blockIds.Contains(currentBlock.BlockId) && (desiredBock.BlockMaterial != EnumBlockMaterial.Wood || currentBlock.Fertility == 0) /* Allow logs to replace soil */)
            {
                return PlaceResumeState.Stop;
            }

            return (desiredBock.Replaceable > currentBlock.Replaceable) ? PlaceResumeState.CannotPlace : PlaceResumeState.CanPlace;
        }
    }

    enum PlaceResumeState
    {
        CannotPlace,
        CanPlace,
        Stop
    }
}
