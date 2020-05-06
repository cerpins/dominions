using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;

namespace dominions.world
{
    internal class Core : ModSystem
    {
        private ICoreAPI api;

        public override void Start(ICoreAPI api)
        {
            this.api = api;
            RegisterBlockBehaviors();
        }

        private void RegisterBlockBehaviors()
        {
            api.RegisterBlockBehaviorClass("FiniteSpreadingWater", typeof(BlockBehaviorFiniteSpreadingWater));
        }
    }
}