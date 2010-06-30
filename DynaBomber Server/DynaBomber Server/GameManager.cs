#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using DynaBomber_Server.Interop.ClientMsg;
using DynaBomber_Server.Interop.ServerMsg;
using ProtoBuf;

#endregion

namespace DynaBomber_Server
{
    /// <summary>
    /// Manages running games on the server
    /// </summary>
    class GameManager
    {
        private const int WaitingGames = 1;
        private const int ServerPort = 4502;

        // Listening socket
        private Socket _serverSocket;

        /// <summary>
        /// Holds list of clients currently in lobby
        /// </summary>
        private readonly List<Socket> _connectedLobbyClients;
        /// <summary>
        /// Holds list of active game objects
        /// </summary>
        private readonly List<Game> _activeGames;

        // Simple game ID counter
        private int _gameIdCounter = 1;

        /// <summary>
        /// Constructor
        /// </summary>
        public GameManager()
        {
            _activeGames = new List<Game>();
            _connectedLobbyClients = new List<Socket>();
        }

        /// <summary>
        /// Main game manager loop
        /// </summary>
        public void Run()
        {
            // Create the listening server socket
            Thread listeningThread = new Thread(SocketListener) {IsBackground = true};
            listeningThread.Start();

            Thread lobbyUpdaterThread = new Thread(LobbyStatusUpdater) {IsBackground = true};
            lobbyUpdaterThread.Start();

            while(true)
            {
                // Check if we have enough idle games

                // Remove defunct active games
                lock(_activeGames)
                {
                    _activeGames.RemoveAll(game => game.Status == GameStatus.Kill);

                    // If there are no empty games available, create a new one
                    int emptyGameCount = _activeGames.Count(game => game.NumClients == 0);

                    // TODO optimise
                    if (emptyGameCount > 1)
                    {
                        _activeGames.RemoveAll(game => game.NumClients == 0);
                        emptyGameCount = 0;
                    }

                    if (emptyGameCount == 0)
                    {
                        Game newGame = new Game(_gameIdCounter++);
                        Thread gameThread = new Thread(newGame.Run);
                        gameThread.Start();

                        _activeGames.Add(newGame);
                    }
                }

                int gameNum = _activeGames.Count;
                int idleNum = _activeGames.Count(game => game.Status == GameStatus.Waiting);
                int players = _activeGames.Sum(game => game.NumClients);

                Console.Title = "Active games: " + gameNum + ", waiting games: " + idleNum + ", players: " + players;

                Thread.Sleep(100);
            }
        }

        /// <summary>
        /// Main socket listener thread 
        /// </summary>
        private void SocketListener()
        {
            // Create the server socket
            _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _serverSocket.Bind(new IPEndPoint(IPAddress.Any, ServerPort));
            _serverSocket.Listen(10);

            while(true)
            {
                Socket connection = _serverSocket.Accept();

                SendGameList(connection);
                // Add client to lobby client list
                lock(_connectedLobbyClients)
                {
                    _connectedLobbyClients.Add(connection);
                }
                // Setup async data receive
                SocketAsyncEventArgs sArgs = new SocketAsyncEventArgs();
                sArgs.SetBuffer(new byte[512], 0, 512);
                sArgs.Completed += ClientMessageReceived;
                sArgs.UserToken = connection;
                connection.ReceiveAsync(sArgs);
            }
        }

        /// <summary>
        /// Sends list of current games to passed client socket
        /// </summary>
        /// <param name="socket">Socket belonging to client</param>
        private void SendGameList(Socket socket)
        {
            ServerGameList gameList = new ServerGameList(_activeGames.Where(game => game.Status == GameStatus.Waiting).ToList<Game>());
            MemoryStream ms = new MemoryStream();
            gameList.Serialize(ms);

            try
            {
                socket.Send(ms.GetBuffer());
            }
            catch (Exception)
            {
                Console.WriteLine("Lost client from lobby.");
            }
            
        }

        private void ClientMessageReceived(object sender, SocketAsyncEventArgs e)
        {
            MemoryStream receivedData = new MemoryStream(e.Buffer, e.Offset, e.BytesTransferred);

            ClientMessageTypes messageType = (ClientMessageTypes) receivedData.ReadByte();

            switch (messageType)
            {
                case ClientMessageTypes.JoinGame:
                    ClientJoinGameRequest joinGameRequest = Serializer.DeserializeWithLengthPrefix<ClientJoinGameRequest>(receivedData, PrefixStyle.Base128);
                    ClientJoinGame(joinGameRequest, (Socket)e.UserToken);
                    break;

                default:
                    Console.WriteLine("Unknown message type received.");
                    break;
            }
        }

        private void ClientJoinGame(ClientJoinGameRequest request, Socket clientSocket)
        {
            IEnumerable<Game> gameList;

            lock (_activeGames)
            {
                gameList = _activeGames.Where(game => (game.ID == request.GameID && game.Status == GameStatus.Waiting));

                // Check if game exists
                if (gameList.Count() < 1)
                    return;

                Game targetGame = gameList.First();

                // Check if game is ready to receive a client
                if (targetGame.Status != GameStatus.Waiting || targetGame.NumClients > 3)
                    return;

                lock(_connectedLobbyClients)
                {
                    _connectedLobbyClients.Remove(clientSocket);
                }

                // Send client a successful join response
                ServerResponse response = new ServerResponse(ServerResponse.Response.JoinOk);
                MemoryStream ms = new MemoryStream();
                response.Serialize(ms);
                clientSocket.Send(ms.GetBuffer());

                targetGame.AddClient(clientSocket, request.PlayerName);
            }
        }

        /// <summary>
        /// Periodically sends current game list to all clients in the lobby
        /// </summary>
        private void LobbyStatusUpdater()
        {
            while(true)
            {
                lock(_connectedLobbyClients)
                {
                    _connectedLobbyClients.RemoveAll(client => !client.Connected);

                    foreach (Socket connectedLobbyClient in _connectedLobbyClients)
                    {
                        SendGameList(connectedLobbyClient);
                    }
                }

                Thread.Sleep(1000);
            }
        }
    }
}
