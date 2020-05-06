using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Server;

namespace dominions.Systems.Maps
{
    /* Handles getting pixels from a Bitmap, where the center of the world
    /* is the center of the bitmap.
    /* 1 px = 1 chunk
    /* 
    /* Use: Inherit and expose color values as meaningful data. 
    */
    public class Map
    {
        Bitmap map;
        // Color when out of bounds
        Color defaultColor;

        // Sizes are relative to chunks, 1px = 1chunk
        int worldX;
        int worldZ;

        int offsetX;
        int offsetZ;

        // Only meant to be inherited
        protected Map(ICoreServerAPI serverAPI, string filename)
        {
            // Always keep all maps in Maps folder
            string folderPath = serverAPI.GetOrCreateDataPath("Maps");
            string filePath = Path.Combine(folderPath, filename);

            // Exception thrown by bitmap is not very clear
            if (!File.Exists(filePath)) throw new FileNotFoundException("Could not resolve path " + filePath);

            map = new Bitmap(filePath);
            defaultColor = Color.Black;

            int chunkSize = serverAPI.WorldManager.ChunkSize;

            // World size in chunks
            worldX = serverAPI.WorldManager.MapSizeX / chunkSize;
            worldZ = serverAPI.WorldManager.MapSizeZ / chunkSize;

            // Distance between map and world start, if map were centered in world
            offsetX = (worldX - map.Width) / 2;
            offsetZ = (worldZ - map.Height) / 2;
        }

        protected Color PixelAtChunk(int chunkX, int chunkZ)
        {
            int mapX = chunkX - offsetX;
            int mapZ = chunkZ - offsetZ;

            // Out of map bounds
            if (mapX >= map.Width || mapZ >= map.Height ||
                mapX < 0 || mapZ < 0) return defaultColor;

            return map.GetPixel(mapX, mapZ);
        }
    }
}
