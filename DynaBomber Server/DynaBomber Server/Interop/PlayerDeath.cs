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
    public class PlayerDeath : IUpdate
    {
        public PlayerDeath()
        {}

        public PlayerDeath(PlayerColors color)
        {
            PlayerColor = color;
        }

        [ProtoMember(1)]
        public PlayerColors PlayerColor { get; set; }

        public void Serialize(MemoryStream ms)
        {
            ms.WriteByte((byte)MessageType.PlayerDeath);
            Serializer.SerializeWithLengthPrefix(ms, this, PrefixStyle.Base128);
        }
    }
}
