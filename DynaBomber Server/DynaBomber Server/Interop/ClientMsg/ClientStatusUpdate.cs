using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace DynaBomber_Server.Interop.ClientMsg
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
    public class ClientStatusUpdate : IClientUpdate
    {
        public ClientStatusUpdate()
        {}

        [ProtoMember(1)]
        public ClientUpdate Update { get; set; }
    }
}
