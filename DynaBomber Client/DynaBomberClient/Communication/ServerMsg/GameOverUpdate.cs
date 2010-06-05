using DynaBomberClient.MainGame.Players;
using ProtoBuf;

namespace DynaBomberClient.Communication.ServerMsg
{
    [ProtoContract]
    public class GameOverUpdate
    {
        [ProtoMember(1)]
        public PlayerColor Winner { get; set; }
    }
}