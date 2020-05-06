using Newtonsoft.Json;
using System.Collections.Generic;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace dominions.Systems.TreeGen
{
    // Subsection of tree gen config
    [JsonObject(MemberSerialization.OptIn)]
    public class TreeBlocks
    {
        [JsonProperty]
        public AssetLocation logBlockCode = null;

        // These can be omitted and left as null
        [JsonProperty]
        public AssetLocation otherLogBlockCode = null;
        [JsonProperty]
        public double otherLogChance = 0.01;
        [JsonProperty]
        public AssetLocation fruitLeavesBlockCode = null;

        [JsonProperty]
        public AssetLocation leavesBlockCode = null;
        [JsonProperty]
        public AssetLocation leavesBranchyBlockCode = null;
        [JsonProperty]
        public AssetLocation vinesBlockCode = null;
        [JsonProperty]
        public AssetLocation vinesEndBlockCode = null;

        public Block vinesBlock;
        public Block vinesEndBlock;
        public int logBlockId;
        public int otherLogBlockId;
        public int leavesBlockId;
        public int leavesBranchyBlockId;
        public int leavesBranchyDeadBlockId;

        public int fruitLeavesBlockId;   

        public HashSet<int> blockIds = new HashSet<int>();

        public void ResolveBlockNames(ICoreServerAPI api)
        {
            int logBlockId = api.WorldManager.GetBlockId(logBlockCode);
            if (logBlockId == -1)
            {
                api.Server.LogWarning("Tree gen tree: No block found with the blockcode " + logBlockCode);
                logBlockId = 0;
            }
            this.logBlockId = logBlockId;

            int leavesBlockId = api.WorldManager.GetBlockId(leavesBlockCode);
            if (leavesBlockId == -1)
            {
                api.Server.LogWarning("Tree gen tree: No block found with the blockcode " + leavesBlockCode);
                leavesBlockId = 0;
            }
            this.leavesBlockId = leavesBlockId;

            int leavesBranchyBlockId = api.WorldManager.GetBlockId(leavesBranchyBlockCode);
            if (leavesBranchyBlockId == -1)
            {
                api.Server.LogWarning("Tree gen tree: No block found with the blockcode " + leavesBranchyBlockCode);
                leavesBranchyBlockId = 0;
            }
            this.leavesBranchyBlockId = leavesBranchyBlockId;

            // Vines
            int vinesBlockId = api.WorldManager.GetBlockId(vinesBlockCode);
            if (vinesBlockId == -1)
            {
                api.Server.LogWarning("Tree gen tree: No block found with the blockcode " + vinesBlockCode);
                vinesBlockId = 0;
            }
            else
            {
                this.vinesBlock = api.World.Blocks[vinesBlockId];
            }

            int vinesEndBlockId = api.WorldManager.GetBlockId(vinesEndBlockCode);
            if (vinesEndBlockId == -1)
            {
                api.Server.LogWarning("Tree gen tree: No block found with the blockcode " + vinesEndBlockCode);
                vinesEndBlockId = 0;
            }
            else
            {
                this.vinesEndBlock = api.World.Blocks[vinesEndBlockId];
            }

            // These can be omitted in json
            int otherLogBlockId = 0;
            if (otherLogBlockCode != null)
            {
                otherLogBlockId = api.WorldManager.GetBlockId(otherLogBlockCode);
                if (otherLogBlockId == -1)
                {
                    api.Server.LogWarning("Tree gen tree: No block found with the blockcode " + otherLogBlockCode);
                    otherLogBlockId = 0;
                }
                this.otherLogBlockId = otherLogBlockId;
            }

            int fruitLeavesBlockId = 0;
            if (fruitLeavesBlockCode != null)
            {
                fruitLeavesBlockId = api.WorldManager.GetBlockId(fruitLeavesBlockCode);
                if (fruitLeavesBlockId == -1)
                {
                    api.Server.LogWarning("Tree gen tree: No block found with the blockcode " + leavesBranchyBlockCode);
                    fruitLeavesBlockId = 0;
                }
                this.fruitLeavesBlockId = fruitLeavesBlockId;
            }
             
            blockIds.Add(leavesBlockId);
            blockIds.Add(leavesBranchyBlockId);
            blockIds.Add(logBlockId);
            blockIds.Add(otherLogBlockId);
            blockIds.Add(fruitLeavesBlockId);
            
        }
    }
}
