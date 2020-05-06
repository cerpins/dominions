using ProtoBuf;

namespace dominions.characters
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class SkinChange
    {
        public string part;
        public string variant;
    }
}
