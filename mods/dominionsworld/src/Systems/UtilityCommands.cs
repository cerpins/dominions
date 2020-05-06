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

namespace dominions.src.Systems
{
    class UtilityCommands : ModSystem
    {
        ICoreServerAPI sapi;
        public override void StartServerSide(ICoreServerAPI sapi)
        {
            base.StartServerSide(sapi);
            this.sapi = sapi;

            sapi.RegisterCommand("getblockid", "Retrieve block id", "/getBlockId string", CmdGetBlockId);
        }
        private void CmdGetBlockId(IServerPlayer player, int groupId, CmdArgs args)
        {
            int cmdChan = GlobalConstants.AllChatGroups;

            if (args.Length < 1) {
                player.SendMessage(cmdChan, "No arguments passed", EnumChatType.CommandError);
                return;
            }

            int blockId = sapi.WorldManager.GetBlockId(new AssetLocation(args[0]));
            player.SendMessage(cmdChan, "Asset id: " + blockId, EnumChatType.CommandSuccess);
        }
    }
}
