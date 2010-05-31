using System.Xml.Serialization;
using DynaBomberClient.Player;
using ProtoBuf;

namespace DynaBomberClient.MainGame.Server
{

    [ProtoContract]
    public class PlayerDeath
    {
        public PlayerDeath()
        { }

        [ProtoMember(1)]
        public PlayerColor PlayerColor { get; set; }
    }

}
