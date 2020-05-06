using System.Collections.Generic;
using Vintagestory.API.Server;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Client;

namespace dominions.characters
{
    public class Core : ModSystem
    {
        #region Config
        public static Dictionary<string, string[]> skinTypes = new Dictionary<string, string[]>()
        {
            ["skincolor"] = new[] { "brown", "light-brown", "olive", "yellow", "tan", "pale" },
            ["eyecolor"] = new[] { "dark-brown", "brown", "light-brown", "dark-green", "light-green", "deep-blue", "light-blue", "grey", "milky" },
            ["haircolor"] = new[] { "black", "dark-brown", "brown", "red", "dark-blonde", "blonde", "white", "grey" },
            ["hairtype"] = new[] { "none", "m", "two", "three", "four", "five", "six", "seven" },
            ["facialhair"] = new[] { "none", "full", "two", "three", "four", "five", "six" },
        };
        #endregion

        ClientSkinNetwork clientSkinNetwork;
        ServerSkinNetwork serverSkinNetwork;

        public override void StartClientSide(ICoreClientAPI api)
        {
            this.clientSkinNetwork = new ClientSkinNetwork(api);

            api.Event.LevelFinalize += () =>
            {
                api.World.Player.Entity.WatchedAttributes.RegisterModifiedListener("race", () =>
                {
                    SetRacials(api.World.Player.Entity);
                });
                SetRacials(api.World.Player.Entity);
            };


            api.RegisterCommand("skin", "Opens skin change gui.", "", (int i, CmdArgs args) =>
            {
                GuiDialogSkin skinGui = new GuiDialogSkin(api, this.clientSkinNetwork);
                skinGui.TryOpen();
            });

            api.RegisterEntityRendererClass("CustomRenderer", typeof(EntityCharacterSkinRenderer));

            base.StartClientSide(api);
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            this.serverSkinNetwork = new ServerSkinNetwork(api);

            api.Event.PlayerJoin += (IServerPlayer player) =>
            {
                player.Entity.WatchedAttributes.RegisterModifiedListener("race", () =>
                {
                    SetRacials(player.Entity);
                });
                SetRacials(player.Entity);
            };

            base.StartServerSide(api);
        }

        private void SetRacials(EntityPlayer entityPlayer)
        {
            switch (entityPlayer.WatchedAttributes.GetString("race", "human"))
            {
                case "dwarf":
                    entityPlayer.Properties.SetEyeHeight(1.11);
                    break;
                case "human":
                default:
                    entityPlayer.Properties.SetEyeHeight(1.7);
                    break;
            }
        }
    }

}
