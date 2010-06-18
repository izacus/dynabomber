using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace DynaBomber_Server.Interop.ClientMsg
{
    [ProtoContract]
    public class ClientJoinGameRequest
    {
        public ClientJoinGameRequest()
        {}

        [ProtoMember(1)]
        public int GameID { get; set; }

        [ProtoMember(2)]
        public string PlayerName { get; set; }
    }
}
