using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace DynaBomber_Server.Interop.ServerMsg
{
    [ProtoContract]
    class GameList : IServerUpdate
    {
        public GameList(List<Game> games)
        {
            List<GameInfo> gameinfos = new List<GameInfo>();

            lock(games)
            {
                foreach (Game game in games)
                {
                    string[] players = new string[4] {"Empty", "Empty", "Empty", "Empty"};

                    for (int i = 0; i < game.NumClients; i++)
                        players[i] = "John Doe";

                    gameinfos.Add(new GameInfo(game.ID, players));

                }
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
    class GameInfo
    {
        public GameInfo(uint id, string[] players)
        {
            this.ID = id;
            this.Players = players;
        }

        [ProtoMember(1)]
        public uint ID { get; set; }
        [ProtoMember(2)]
        public string[] Players { get; set; }
    }
}
