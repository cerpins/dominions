using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;

namespace dmutils.src
{
    class ConfigureWorld : ModSystem
    {
        public override double ExecuteOrder()
        {
            return 0;
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);

            TerraGenConfig.geoProvMapScale = 128; // For ores I assume? Ask Archpriest
            setConfig(api.World.Config);
        }

        public void setConfig(ITreeAttribute config)
        {
            config.SetBool("allowMap", false);
            config.SetBool("allowCoordinateHud", false);
            config.SetBool("allowLandClaiming", false);
            config.SetBool("temporalStability", false);
            config.SetString("temporalStorms", "off");
            config.SetString("creatureStrength", "0.7");
            config.SetBool("microblockChiseling", true);
            config.SetString("playerHungerSpeed", "0.45");
            config.SetString("blockGravity", "sandgravelsoil");
            config.SetString("foodSpoilSpeed", "1.2");
            config.SetString("toolMiningSpeed", "0.15");
            config.SetString("toolDurability", "1.3");
        }
    }

    class Core : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterItemClass("dmItemAxeLowerDrops", typeof(ItemAxeLowerDrops));
        }
    }
}
