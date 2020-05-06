using dominions.Systems.Maps;
using dominions.world;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace dominions.Systems.TreeGen
{
    public class ForestSupplier
    {
        public TreeGenerators treeGenerators;
        ICoreServerAPI serverApi;

        ForestConfig forestConfig;
        public MapForest mapForest; 

        public ForestSupplier(ICoreServerAPI serverApi)
        {
            this.serverApi = serverApi;

            mapForest = WorldMap.TryGet(serverApi).mapForest;
            treeGenerators = new TreeGenerators(serverApi);
        }

        public void LoadForest()
        {
            // Load respective configurations
            forestConfig = serverApi.Assets.Get("dominions:worldgen/forests.json").ToObject<ForestConfig>();
            treeGenerators.LoadTreeGenerators();
        }

        public ForestChunk GetForestAtChunk(int chunkX, int chunkZ)
        {
            ForestValues values = mapForest.GetValues(chunkX, chunkZ);

            // Out of variant bounds
            if (values.forestIndex > forestConfig.Variants.Length - 1) return null;

            ForestVariant variant = forestConfig.Variants[values.forestIndex];
            // Multiplier * Initial density
            float treeTries = values.density * variant.Density;

            return new ForestChunk(variant.TreeProps, (int)treeTries, values);
        }
    }
}
