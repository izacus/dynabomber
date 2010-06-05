using System.IO;
using DynaBomberClient.MainGame.Players;
using ProtoBuf;

namespace DynaBomberClient.Communication.ClientMsg
{
    [ProtoContract]
    public class ClientPositionUpdate : IClientMessage
    {
        public ClientPositionUpdate(int x, int y, MovementDirection direction, bool moving)
        {
            this.X = x;
            this.Y = y;
            this.Direction = direction;
            this.Moving = moving;
        }

        [ProtoMember(1)]
        public int X { get; set; }
        [ProtoMember(2)]
        public int Y { get; set; }
        [ProtoMember(3)]
        public MovementDirection Direction { get; set; }
        [ProtoMember(4)]
        public bool Moving { get; set; }

        public void Serialize(MemoryStream ms)
        {
            ms.WriteByte((byte)ClientMessageTypes.PositionUpdate);
            Serializer.SerializeWithLengthPrefix(ms, this, PrefixStyle.Base128);
        }
    }
}
