using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.ServerMods;
using Vintagestory.GameContent;
using Vintagestory.API.Config;

namespace dominions.world
{
    public class DisableVanillaHandlers : ModSystem
    {
        private ICoreServerAPI api;

        public override double ExecuteOrder()
        {
            RuntimeEnv.DebugOutOfRangeBlockAccess = true;
            return 0;
        }

        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return forSide == EnumAppSide.Server;
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            this.api = api;
            api.Event.ServerRunPhase(EnumServerRunPhase.LoadGamePre, DisableHandlers);
        }

        void DisableHandlers()
        {
            string[] worldGenWipes = new string[] {"GenBlockLayers", "GenTerra", "GenCaves", "GenMaps", "GenVegetation"};
            string[] mapRegionWipes = new string[] {"GenBlockLayers", "GenMaps"};

            IWorldGenHandler handlerGroups = api.Event.GetRegisteredWorldGenHandlers("standard");
            
            var worldGenPasses = handlerGroups.OnChunkColumnGen;
            foreach (var handlers in worldGenPasses)
            {
                for (int h = 0; handlers != null && h < handlers.Count; h++)
                {
                    var handler = handlers[h];

                    var targetType = handler.Target.GetType();

                    if (targetType.Namespace != "Vintagestory.ServerMods" || !worldGenWipes.Contains(targetType.Name)) continue;
                    handlers.RemoveAt(h--);
                }
            }

            var mapRegionHandlers = handlerGroups.OnMapRegionGen;
            for (int h = 0; mapRegionHandlers != null && h < mapRegionHandlers.Count; h++)
            {
                var handler = mapRegionHandlers[h];
                var targetType = handler.Target.GetType();

                if (targetType.Namespace != "Vintagestory.ServerMods" || !mapRegionWipes.Contains(targetType.Name)) continue;
                mapRegionHandlers.RemoveAt(h--);
            }

        }
    }
}
