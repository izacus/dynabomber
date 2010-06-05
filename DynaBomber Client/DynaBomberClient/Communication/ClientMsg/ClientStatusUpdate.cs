using System.IO;
using ProtoBuf;

namespace DynaBomberClient.Communication.ClientMsg
{
    public enum ClientUpdate
    {
        MapOk = 0,
        PlayerInfoOk = 1,
        Ready = 2,
        BombTrigger = 3,
        GameOverOk = 4
    }

    [ProtoContract]
    public class ClientStatusUpdate : IClientMessage
    {
        public ClientStatusUpdate(ClientUpdate updateType)
        {
            this.Update = updateType;
        }

        [ProtoMember(1)]
        public ClientUpdate Update { get; set; }

        public void Serialize(MemoryStream ms)
        {
            ms.WriteByte((byte)ClientMessageTypes.StatusUpdate);
            Serializer.SerializeWithLengthPrefix(ms, this, PrefixStyle.Base128);
        }
    }
}
