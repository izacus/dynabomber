using ProtoBuf;

namespace DynaBomberClient.Communication.ServerMsg
{
    public enum ServerCommand
    {
        StartGame,
        PlayerUpdate,
        BombSet,
        ClearPowerup,
        ScrambleControls
    }

    [ProtoContract]
    public class ServerStatusUpdate
    {
        public ServerStatusUpdate()
        {
            
        }

        [ProtoMember(1)]
        public PlayerInfo[] Players { get; set; }

        [ProtoMember(2)]
        public ServerCommand Update { get; set; }

        [ProtoMember(3)]
        public int X { get; set; }

        [ProtoMember(4)]
        public int Y { get; set; }
    }
}
