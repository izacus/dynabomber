using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DynaBomber_Server.Interop.ServerMsg;
using ProtoBuf;

namespace DynaBomber_Server.Interop.ClientMsg
{
    [ProtoContract]
    public class ClientPositionUpdate : IClientUpdate
    {
        public ClientPositionUpdate()
        {}

        [ProtoMember(1)]
        public int X { get; set; }
        [ProtoMember(2)]
        public int Y { get; set; }
        [ProtoMember(3)]
        public MovementDirection Direction { get; set; }
        [ProtoMember(4)]
        public bool Moving { get; set; }
    }
}
