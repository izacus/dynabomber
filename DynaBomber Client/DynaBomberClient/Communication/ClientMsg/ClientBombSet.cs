using System.IO;
using ProtoBuf;

namespace DynaBomberClient.Communication.ClientMsg
{
    [ProtoContract]
    public class ClientBombSet : IClientMessage
    {
        public ClientBombSet(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        [ProtoMember(1)]
        public int X { get; set; }
        [ProtoMember(2)]
        public int Y { get; set;}

        public void Serialize(MemoryStream ms)
        {
            ms.WriteByte((byte)ClientMessageTypes.BombSet);
            Serializer.SerializeWithLengthPrefix(ms, this, PrefixStyle.Base128);
        }
    }
}
