using Vintagestory.API.Client;

namespace dominions.characters
{
    public class ClientSkinNetwork
    {
        ICoreClientAPI capi;
        IClientNetworkChannel clientNetworkChannel;

        public ClientSkinNetwork(ICoreClientAPI api)
        {
            this.capi = api;

            this.clientNetworkChannel = api.Network.RegisterChannel("playerskins");
            this.clientNetworkChannel.RegisterMessageType(typeof(SkinChange));
        }

        public void SendSkinPacket(string newpart, string newvariant)
        {
            clientNetworkChannel.SendPacket(new SkinChange()
            {
                part = newpart,
                variant = newvariant
            });
        }
    }
}
