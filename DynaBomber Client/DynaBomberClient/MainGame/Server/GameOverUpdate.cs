using System.Xml.Serialization;
using DynaBomberClient.Player;
using ProtoBuf;

namespace DynaBomberClient.MainGame.Server
{
    [ProtoContract]
    public class GameOverUpdate
    {
        [ProtoMember(1)]
        public PlayerColor Winner { get; set; }
    }
}