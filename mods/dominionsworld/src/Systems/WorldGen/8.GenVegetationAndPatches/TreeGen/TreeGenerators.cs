using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace dominions.Systems.TreeGen
{
    public class TreeGenerators
    {
        ICoreServerAPI sApi;

        public TreeGenerators(ICoreServerAPI sApi) { this.sApi = sApi; }

        public void Reload()
        {
            ReloadTreeGenerators();
            LoadTreeGenerators();
        }

        public void ReloadTreeGenerators()
        {
            int quantity = sApi.Assets.Reload(new AssetLocation("dominions:worldgen/treegen/"));
            sApi.Server.LogEvent("Dominions reloaded {0} tree generators", quantity);

            LoadTreeGenerators();
        }

        public void LoadTreeGenerators()
        {
            // Use custom tree folder
            string basePath = "worldgen/treegen/";
            Dictionary<AssetLocation, TreeGenConfig> treeGens = sApi.Assets.GetMany<TreeGenConfig>(sApi.Server.Logger, basePath, "dominions");

            // Make sure this is executed after vanilla binds
            sApi.World.TreeGenerators.Clear();

            string names = "";
            foreach (var val in treeGens)
            {
                AssetLocation location = val.Key;
                TreeGenConfig tree = val.Value;

                // Name is TreeGen [subfolder/filename] with no extension
                AssetLocation name = location.Clone();
                name.Path = location.Path.Substring("worldgen/treegen/".Length);
                name.RemoveEnding();

                tree.Init(location, sApi.Server.Logger);

                sApi.RegisterTreeGenerator(name, new TreeGen(val.Value, sApi.WorldManager.Seed));
                val.Value.treeBlocks.ResolveBlockNames(sApi);
            }

            sApi.Server.LogNotification("Reloaded {0} tree generators - " + names, treeGens.Count);
        }

        public ITreeGenerator GetGenerator(AssetLocation generatorCode)
        {
            ITreeGenerator gen;
            sApi.World.TreeGenerators.TryGetValue(generatorCode, out gen);
            return gen;
        }

        public KeyValuePair<AssetLocation, ITreeGenerator> GetGenerator(int index)
        {
            AssetLocation key = sApi.World.TreeGenerators.GetKeyAtIndex(index);
            if (key != null)
            {
                return new KeyValuePair<AssetLocation, ITreeGenerator>(key, sApi.World.TreeGenerators[key]);
            }
            return new KeyValuePair<AssetLocation, ITreeGenerator>(null, null);
        }

        public void RunGenerator(AssetLocation treeName, IBlockAccessor api, BlockPos pos, float size = 1)
        {
            sApi.World.TreeGenerators[treeName].GrowTree(api, pos, size);
        }
    }
}
