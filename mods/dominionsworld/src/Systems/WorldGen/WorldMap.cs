using dominions.Systems.Maps;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.ServerMods;
using Vintagestory.ServerMods.NoObf;

namespace dominions.world
{
    public class WorldMap
    {
        private static WorldMap singleton;
        public static WorldMap TryGet(ICoreServerAPI api)
        {
            return singleton ?? (singleton = new WorldMap(api));
        }

        ICoreServerAPI serverAPI;

        public MapPhysical mapPhysical;
        public MapForest mapForest;

        public WorldMap(ICoreServerAPI serverAPI)
        {
            this.serverAPI = serverAPI;
            string mapFolder = serverAPI.GetOrCreateDataPath("Maps");
            mapPhysical = new MapPhysical(serverAPI);
            mapForest = new MapForest(serverAPI);

            serverAPI.RegisterCommand("getmap", "Gets pixel value on mapPhysical", "/getmap [mapname]", CmdGetMap);
        }

        public int[] GenClimateLayer(int regionX, int regionZ, int sizeX, int sizeZ)
        {
            int[] result = new int[sizeX * sizeZ];

            for (int z = 0; z < sizeZ; z++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    int chunkX = regionX + x;
                    int chunkZ = regionZ + z;

                    PhysicalValues values = mapPhysical.GetValues(chunkX, chunkZ);
                    int climate = (values.temperature << 16) + (values.humidity << 8);

                    // Select right, bottom, rightbottom location in flattened 2d array
                    int index2d = z * sizeX + x;
                    result[index2d] = climate;
                }
            }

            return result;
        }

        public int[] GenLandformLayer(int regionX, int regionZ, int sizeX, int sizeZ)
        {
            int[] result = new int[sizeX * sizeZ];

            int chunkStartX = regionX / 2;
            int chunkStartZ = regionZ / 2;

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    // Skip every 2nd itteration on x and y
                    if ((x % 2) != 0 || (z % 2) != 0) continue;

                    int chunkX = chunkStartX + x / 2;
                    int chunkZ = chunkStartZ + z / 2;
                    int landform = mapPhysical.GetLandformIndex(chunkX, chunkZ);

                    // Select right, bottom, rightbottom location in flattened 2d array
                    int index2d = z * sizeX + x;
                    result[index2d] = landform;
                    result[index2d + 1] = landform;
                    result[index2d + sizeX] = landform;
                    result[index2d + sizeX + 1] = landform;
                }
            } 

            return result;
        }
        public int[] GenForestLayer(int regionX, int regionZ, int sizeX, int sizeZ)
        {
            int[] result = new int[sizeX * sizeZ];

            int chunkStartX = regionX * 16;
            int chunkStartZ = regionZ * 16;

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    int chunkX = chunkStartX + x;
                    int chunkZ = chunkStartZ + z;

                    int forest = mapForest.GetDensity(chunkX, chunkZ);
                    result[z * sizeX + x] = forest;
                }
            }

            return result;
            //forestGen.GenLayer(regionX * noiseSizeForest, regionZ * noiseSizeForest, noiseSizeForest + 1, noiseSizeForest + 1);
        }

        public static int ColorToInt(Color color)
        {
            return (color.R << 16) + (color.G << 8) + color.B;
        }

        void CmdGetMap(IServerPlayer player, int groupId, CmdArgs args)
        {
            int chunkX = player.Entity.ServerPos.AsBlockPos.X / 32;
            int chunkZ = player.Entity.ServerPos.AsBlockPos.Z / 32;

            int cmdChan = GlobalConstants.AllChatGroups;
            if (args.Length == 0)
            {
                player.SendMessage(cmdChan, "Map name not specified", EnumChatType.CommandError);
                return;
            } 

            if (args[0] == "physical")
            {
                PhysicalValues values = mapPhysical.GetValues(chunkX, chunkZ);

                player.SendMessage(cmdChan, "Temperature: " + values.temperature, EnumChatType.Notification);
                player.SendMessage(cmdChan, "Humidity: " + values.humidity, EnumChatType.Notification);
                player.SendMessage(cmdChan, "Landform index: " + values.landformIndex, EnumChatType.Notification);
                return;
            }

            if (args[0] == "forest")
            {
                ForestValues values = mapForest.GetValues(chunkX, chunkZ);

                player.SendMessage(cmdChan, "Forest index: " + values.forestIndex, EnumChatType.Notification);
                player.SendMessage(cmdChan, "Density: " + values.density, EnumChatType.Notification);
                return;
            }

            player.SendMessage(cmdChan, "Map does not exist", EnumChatType.CommandError);
        }
    }
}
