using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Server;

namespace dominions.Systems.Maps
{
    public struct PhysicalValues
    {
        public int temperature;
        public int humidity;
        public int landformIndex;
    }

    public class MapPhysical : Map
    {
        public MapPhysical(ICoreServerAPI api) : base(api, "physical.png")
        {
        }

        public PhysicalValues GetValues(int chunkX, int chunkZ)
        {
            Color pixel = PixelAtChunk(chunkX, chunkZ);

            return new PhysicalValues()
            {
                temperature = pixel.R,
                humidity = pixel.G,
                landformIndex = pixel.B
            };
        }

        public int GetTemperature(int chunkX, int chunkZ) 
        { 
            return PixelAtChunk(chunkX, chunkZ).R; 
        }
        public int GetHumidity(int chunkX, int chunkZ) 
        { 
            return PixelAtChunk(chunkX, chunkZ).G; 
        }
        public int GetLandformIndex(int chunkX, int chunkZ) 
        { 
            return PixelAtChunk(chunkX, chunkZ).B; 
        }
    }
}
