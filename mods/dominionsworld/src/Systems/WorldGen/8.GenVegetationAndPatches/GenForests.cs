using dominions.Systems.Maps;
using dominions.Systems.TreeGen;
using dominions.world;
using System;
using System.Collections.Generic;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.ServerMods;
using Vintagestory.ServerMods.NoObf;

namespace dominions.Systems
{
    public class GenForests : ModStdWorldGen
    {
        ICoreServerAPI sApi;
        LCGRandom rnd;
        IBlockAccessor blockAccessor;
        int worldheight;

        WorldMap map;
        ForestSupplier forestSupplier;

        public override bool ShouldLoad(EnumAppSide side)
        {
            return side == EnumAppSide.Server;
        }
        public override double ExecuteOrder()
        {
            // Righ after Vanilla to guarantee correct event order
            return 0.51; 
        }

        public override void StartServerSide(ICoreServerAPI sApi)
        {
            this.sApi = sApi;
            forestSupplier = new ForestSupplier(sApi);

            if (!DoDecorationPass) return;

            sApi.Event.InitWorldGenerator(InitWorldGen, "standard");
            sApi.Event.InitWorldGenerator(InitWorldGen, "superflat");
            sApi.Event.ChunkColumnGeneration(OnChunkColumnGen, EnumWorldGenPass.Vegetation, "standard");
            sApi.Event.GetWorldgenBlockAccessor(OnWorldGenBlockAccessor);

            // Register only when supplier has loaded
            sApi.RegisterCommand("tree", "Place a tree from custom treegen", "/tree [folder] [tree_name] [tree_size, default is 0]", CmdTree);
            sApi.RegisterCommand("reload", "Reload tree gen", "", CmdReload);
        }

        private void OnWorldGenBlockAccessor(IChunkProviderThread chunkProvider)
        {
            blockAccessor = chunkProvider.GetBlockAccessor(true);
        }
        
        public void InitWorldGen()
        {
            LoadGlobalConfig(sApi);

            rnd = new LCGRandom(sApi.WorldManager.Seed - 87698);
            chunksize = sApi.WorldManager.ChunkSize;

            map = WorldMap.TryGet(sApi);
            forestSupplier.LoadForest();
        
            worldheight = sApi.WorldManager.MapSizeY; 
        }


        ushort[] heightmap;
        BlockPos tmpPos = new BlockPos();

        private void OnChunkColumnGen(IServerChunk[] chunks, int chunkX, int chunkZ, ITreeAttribute chunkGenParams = null)
        {
            rnd.InitPositionSeed(chunkX, chunkZ);

            heightmap = chunks[0].MapChunk.RainHeightMap;
            GenTrees(chunkX, chunkZ);
            GenShrubs(chunkX, chunkZ);
        }

        void GenTrees(int chunkX, int chunkZ)
        {
            ForestChunk forest = forestSupplier.GetForestAtChunk(chunkX, chunkZ);
            PhysicalValues physValues = map.mapPhysical.GetValues(chunkX, chunkZ);

            if (forest == null) return;

            Block block;
            int dx, dz, x, z;

            List<KeyValuePair<Vec2f, int>> treesWithDist = new List<KeyValuePair<Vec2f, int>>();
            //List<TreeArea> treeAreas = new List<TreeArea>();

            int triesTrees = forest.treeTries;
            while (triesTrees > 0)
            {
                triesTrees--;

                dx = rnd.NextInt(chunksize);
                dz = rnd.NextInt(chunksize);
                x = dx + chunkX * chunksize;
                z = dz + chunkZ * chunksize;

                if (blockAccessor.GetBlock(tmpPos.X, tmpPos.Y, tmpPos.Z).Replaceable >= 6000) tmpPos.Y--;

                int y = heightmap[dz * chunksize + dx];
                if (y <= 0 || y >= worldheight - 50) continue;

                tmpPos.Set(x, y, z);
                block = blockAccessor.GetBlock(tmpPos);

                // Skip blocks that are not on dirt
                if (block.Fertility == 0) continue;

                ForestTreeProperties forestTree = forest.GetTree();
                if (forestTree == null) continue;

                // Distance 0 is usually used for shrubbery
                if (!forestTree.IsShrub)
                {
                    // Padding around borders of chunk
                    // Halving as a temporary adjustment to appeareance 
                    if (dx < forestTree.SideDistance || dx > chunksize - forestTree.SideDistance ||
                        dz < forestTree.SideDistance || dz > chunksize - forestTree.SideDistance) continue;

                    // Expensive, but breaks early and limited by chunk size - maybe not expensive?
                    bool treesOverlap = false;
                    foreach (KeyValuePair<Vec2f, int> posWithDist in treesWithDist)
                    {
                        // Distance needed to place
                        int needDist = forestTree.Distance + posWithDist.Value;

                        float diffX = Math.Abs(tmpPos.X - posWithDist.Key.X);
                        float diffZ = Math.Abs(tmpPos.Z - posWithDist.Key.Y);

                        float dist = GameMath.FastSqrt(diffX * diffX + diffZ * diffZ);

                        if (Math.Floor(dist) >= needDist) continue;

                        treesOverlap = true;
                        break;
                    }

                    if (treesOverlap) continue;

                    treesWithDist.Add(new KeyValuePair<Vec2f, int>(
                        new Vec2f(tmpPos.X, tmpPos.Z),
                        forestTree.Distance
                    ));
                }

                float humidity = physValues.humidity / 255f;
                float temperature = physValues.temperature / 255f;
                float vineChance = (humidity > 0.7f && temperature > 0.4f) ? humidity * temperature : 0;

                ITreeGenerator treeGen = forestSupplier.treeGenerators.GetGenerator(forestTree.Code);
                treeGen.GrowTree(blockAccessor, tmpPos, forestTree.Size.nextFloat(1, rnd), vineChance);
            }
        }

        void GenShrubs(int chunkX, int chunkZ)
        {

        }

        private void CmdTree(IServerPlayer player, int groupId, CmdArgs args)
        {
            int cmdChan = GlobalConstants.AllChatGroups;

            BlockPos pos = player.CurrentBlockSelection.Position;
            if (pos == null)
            {
                player.SendMessage(cmdChan, "No block selected", EnumChatType.CommandError);
                return;
            }

            string code = "dominions:" + args[0] + "/" + args[1];
            AssetLocation tryAsset = new AssetLocation(code);

            // Make sure to pass the correct blockAcessor
            ITreeGenerator treeGen = forestSupplier.treeGenerators.GetGenerator(tryAsset);
            if (treeGen == null)
            {
                player.SendMessage(cmdChan, "Tree does not exist", EnumChatType.CommandError);
                return;
            }

            float size = 1;
            if (args.Length > 2) size = float.TryParse(args[2], out size) ? size : 1;

            float vineChance = 0;
            if (args.Length > 3) size = float.TryParse(args[3], out vineChance) ? vineChance : 0;

            treeGen.GrowTree(sApi.World.BlockAccessor, pos, size, vineChance);
            player.SendMessage(GlobalConstants.AllChatGroups, "Grew tree", EnumChatType.CommandSuccess);
        }

        private void CmdReload(IServerPlayer player, int groupId, CmdArgs args)
        {
            forestSupplier.treeGenerators.Reload();

            player.SendMessage(GlobalConstants.AllChatGroups, "Reloaded trees", EnumChatType.Notification);
        }
    }
}
