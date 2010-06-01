using DynaBomberClient.MainGame.Players;
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
