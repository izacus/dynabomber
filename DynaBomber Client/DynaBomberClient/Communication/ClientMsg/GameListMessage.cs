﻿using System.Collections.Generic;
using ProtoBuf;

namespace DynaBomberClient.Communication.ClientMsg
{
    [ProtoContract]
    public class GameListMessage
    {
        public GameListMessage()
        {}

        [ProtoMember(1)]
        public List<GameInfo> Games { get; set; }
    }

    [ProtoContract]
    public class GameInfo
    {
        public GameInfo()
        {
        }

        [ProtoMember(1)]
        public int ID { get; set; }
        [ProtoMember(2)]
        public string[] Players { get; set; }
    }
}
