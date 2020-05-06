using Vintagestory.API.Server;

namespace dominions.characters
{
    public class ServerSkinNetwork
    {
        ICoreServerAPI sapi;
        IServerNetworkChannel serverNetworkChannel;

        public ServerSkinNetwork(ICoreServerAPI api)
        {
            this.sapi = api;

            this.serverNetworkChannel = api.Network.RegisterChannel("playerskins");
            this.serverNetworkChannel.RegisterMessageType(typeof(SkinChange));
            this.serverNetworkChannel.SetMessageHandler<SkinChange>(SaveSkin);
        }

        public void SaveSkin(IServerPlayer player, SkinChange change)
        {
            player.Entity.WatchedAttributes.SetString(change.part, change.variant);
        }
    }
}
