using System.IO;
using ProtoBuf;

namespace DynaBomber_Server.Interop.ServerMsg
{
    [ProtoContract]
    public class ServerPlayerDeath : IServerUpdate
    {
        public ServerPlayerDeath()
        {}

        public ServerPlayerDeath(PlayerColors color)
        {
            PlayerColor = color;
        }

        [ProtoMember(1)]
        public PlayerColors PlayerColor { get; set; }

        public void Serialize(MemoryStream ms)
        {
            ms.WriteByte((byte)ServerMessageTypes.PlayerDeath);
            Serializer.SerializeWithLengthPrefix(ms, this, PrefixStyle.Base128);
        }
    }
}
