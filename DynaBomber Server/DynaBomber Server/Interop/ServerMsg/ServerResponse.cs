using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace DynaBomber_Server.Interop.ServerMsg
{

    /// <summary>
    /// A simple server status response message
    /// </summary>
    [ProtoContract]
    public class ServerResponse : IServerUpdate
    {
        public enum Response
        {
            JoinOk
        }

        public ServerResponse(Response response)
        {
            this.Value = response;
        }

        [ProtoMember(1)]
        public Response Value { get; set; }

        public void Serialize(MemoryStream ms)
        {
            ms.WriteByte((byte)ServerMessageTypes.SimpleResponse);
            Serializer.SerializeWithLengthPrefix(ms, this, PrefixStyle.Base128);
        }
    }
}
