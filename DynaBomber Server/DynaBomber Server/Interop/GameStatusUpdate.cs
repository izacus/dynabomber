using System;
using System.IO;
using System.Xml.Serialization;
using DynaBomber_Server.GameClasses;
using ProtoBuf;

namespace DynaBomber_Server.Interop
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
    public class GameStatusUpdate : IUpdate
    {
        public GameStatusUpdate()
        {}

        public GameStatusUpdate(Player[] players, Command command)
        {
            this.Players = players;
            this.Update = command;
        }

        public GameStatusUpdate(Player[] players, Command command, int x, int y)
        {
            this.Players = players;
            this.Update = command;
            this.X = x;
            this.Y = y;
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
            ms.WriteByte((byte)MessageType.StatusUpdate);
            Serializer.SerializeWithLengthPrefix(ms, this, PrefixStyle.Base128);
        }
    }
}
