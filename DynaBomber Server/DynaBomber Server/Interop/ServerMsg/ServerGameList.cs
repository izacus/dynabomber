using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace DynaBomber_Server.Interop.ServerMsg
{
    [ProtoContract]
    public class ServerGameList : IServerUpdate
    {
        public ServerGameList(List<Game> games)
        {
            List<GameInfo> gameinfos = new List<GameInfo>();

            lock(games)
            {
                gameinfos.AddRange(games.Select(game => new GameInfo(game.ID, game.ClientNames.ToArray())));
            }

            this.Games = gameinfos;
        }

        [ProtoMember(1)]
        public List<GameInfo> Games { get; set; }

        public void Serialize(MemoryStream ms)
        {
            ms.WriteByte((byte)ServerMessageTypes.GameList);
            Serializer.SerializeWithLengthPrefix(ms, this, PrefixStyle.Base128);
        }
    }

    [ProtoContract]
    public class GameInfo
    {
        public GameInfo(int id, string[] players)
        {
            this.ID = id;
            this.Players = players;
        }

        [ProtoMember(1)]
        public int ID { get; set; }
        [ProtoMember(2)]
        public string[] Players { get; set; }
    }
}
