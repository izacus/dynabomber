using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace DynaBomber_Server.Interop.ClientMsg
{
    [ProtoContract]
    public class ClientBombSet : IClientUpdate
    {
        public ClientBombSet()
        {}

        [ProtoMember(1)]
        public int X { get; set; }
        [ProtoMember(2)]
        public int Y { get; set; }
    }
}
