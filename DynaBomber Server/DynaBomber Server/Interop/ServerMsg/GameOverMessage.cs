using System.IO;
using ProtoBuf;

namespace DynaBomber_Server.Interop.ServerMsg
{
    [ProtoContract]
    public class GameOverMessage : IServerUpdate
    {
        public GameOverMessage()
        {}

        public GameOverMessage(PlayerColors winner)
        {
            Winner = winner;
        }

        [ProtoMember(1)]
        public PlayerColors Winner { get; set; }

        public void Serialize(MemoryStream ms)
        {
            ms.WriteByte((byte)ServerMessageTypes.GameOver);
            Serializer.SerializeWithLengthPrefix(ms, this, PrefixStyle.Base128);
        }
    }
}
