using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ProtoBuf;

namespace DynaBomber_Server.Interop
{
    [ProtoContract]
    public class GameOverMessage : IUpdate
    {
        public GameOverMessage()
        {}

        public GameOverMessage(PlayerColors winner)
        {
            this.Winner = winner;
        }

        [ProtoMember(1)]
        public PlayerColors Winner { get; set; }

        public void Serialize(MemoryStream ms)
        {
            ms.WriteByte((byte)MessageType.GameOver);
            Serializer.SerializeWithLengthPrefix(ms, this, PrefixStyle.Base128);
        }
    }
}
