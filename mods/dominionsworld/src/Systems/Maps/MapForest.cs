using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Server;

namespace dominions.Systems.Maps
{
    public struct ForestValues
    {
        public int forestIndex;
        public int density;
    }

    public class MapForest : Map
    {
        public MapForest(ICoreServerAPI api) : base(api, "forest.png")
        {
        }

        public ForestValues GetValues(int chunkX, int chunkZ)
        {
            Color pixel = PixelAtChunk(chunkX, chunkZ);

            return new ForestValues()
            {
                forestIndex = pixel.R,
                density = pixel.G
            };
        }

        public int GetForestIndex(int chunkX, int chunkZ)
        {
            return PixelAtChunk(chunkX, chunkZ).R;
        }
        public int GetDensity(int chunkX, int chunkZ)
        {
            return PixelAtChunk(chunkX, chunkZ).G;
        }
    }
}
