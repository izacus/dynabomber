using System;
using System.Xml.Serialization;
using DynaBomberClient.Player;
using ProtoBuf;

namespace DynaBomberClient.MainGame.Server
{
    // Deserialized player information class
    [ProtoContract]
    public class PlayerInfo
    {
        public PlayerInfo()
        {
        }

        [ProtoMember(1)]
        public PlayerColor Color { get; set; }

        [ProtoMember(2)]
        public int X { get; set; }

        [ProtoMember(3)]
        public int Y { get; set; }

        [ProtoMember(4)]
        public MovementDirection Direction { get; set; }

        [ProtoMember(5)]
        public Boolean Moving { get; set; }
    }
}
