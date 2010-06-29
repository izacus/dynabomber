using System.IO;
using ProtoBuf;

namespace DynaBomber_Server.Interop.ServerMsg
{
    public enum Command
    {
        StartGame,
        PlayerUpdate,
        BombSet,
        ClearPowerup,
        ScrambleControls
    }
    /// <summary>
    /// Carries update of current game status, including all players and command to current player
    /// </summary>
    [ProtoContract]
    public class ServerGameStatusUpdate : IServerUpdate
    {
        public ServerGameStatusUpdate()
        {}

        public ServerGameStatusUpdate(Player[] players, Command command)
        {
            Players = players;
            Update = command;
        }

        public ServerGameStatusUpdate(Player[] players, Command command, int x, int y)
        {
            Players = players;
            Update = command;
            X = x;
            Y = y;
        }

       [ProtoMember(1)]
       public Player[] Players { get; set; }

       [ProtoMember(2)]
       public Command Update { get; set; }

       [ProtoMember(3)]
       public int X { get; set; }

       [ProtoMember(4)]
       public int Y { get; set; }

        public void Serialize(MemoryStream ms)
        {
            ms.WriteByte((byte)ServerMessageTypes.StatusUpdate);
            Serializer.SerializeWithLengthPrefix(ms, this, PrefixStyle.Base128);
        }
    }
}
