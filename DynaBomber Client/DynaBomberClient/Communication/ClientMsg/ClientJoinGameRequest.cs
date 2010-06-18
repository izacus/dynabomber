using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using ProtoBuf;

namespace DynaBomberClient.Communication.ClientMsg
{
    [ProtoContract]
    public class ClientJoinGameRequest : IClientMessage
    {
        public ClientJoinGameRequest(int gameId, string playerName)
        {
            this.GameID = gameId;
            this.PlayerName = playerName;
        }

        [ProtoMember(1)]
        public int GameID { get; set; }

        [ProtoMember(2)]
        public string PlayerName { get; set; }

        public void Serialize(MemoryStream ms)
        {
            ms.WriteByte((byte)ClientMessageTypes.JoinGame);
            Serializer.SerializeWithLengthPrefix(ms, this, PrefixStyle.Base128);
        }
    }
}
