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
    // Utility class to retrieve forest info within a chunk
    public class ForestChunk
    {
        ForestTreeProperties[] treeProperties;
        public int treeTries;
        public ForestValues values;

        Random rnd;
        int[] totalWeights;

        public ForestChunk(ForestTreeProperties[] treeProperties, int treeTries, ForestValues values)
        {
            this.treeProperties = treeProperties;
            this.treeTries = treeTries;
            this.values = values;

            // Sequential store of weights
            totalWeights = new int[treeProperties.Length];
            int lastWeight = 0;

            for (int t = 0; t < totalWeights.Length; t++)
            {
                // Store ranges of weight
                int weight = treeProperties[t].Weight + lastWeight;
                totalWeights[t] = weight;

                lastWeight = weight;
            }

            rnd = new Random();
        }

        // Retrieve a random tree in chunk
        public ForestTreeProperties GetTree()
        {
            int rndWeight = rnd.Next(totalWeights.Last());
            int lastWeight = 0;
            for (int t = 0; t < totalWeights.Length; t++)
            {
                // Check if draw
                if (rndWeight >= lastWeight && 
                    rndWeight <= totalWeights[t]) return treeProperties[t];
                
                lastWeight = totalWeights[t];
            }

            return null;
        }
    }
}
