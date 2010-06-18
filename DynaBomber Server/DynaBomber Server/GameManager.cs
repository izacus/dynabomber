﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using DynaBomber_Server.Interop.ClientMsg;
using DynaBomber_Server.Interop.ServerMsg;

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

        private List<Socket> _connectedLobbyClients;

        /// <summary>
        /// Holds list of active game objects
        /// </summary>
        private readonly List<Game> _activeGames;
        private List<Game> _idleGames;

        // Simple game ID counter
        private int _gameIdCounter = 1;

        /// <summary>
        /// Constructor
        /// </summary>
        public GameManager()
        {
            _activeGames = new List<Game>();
            _idleGames = new List<Game>();
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

            while(true)
            {
                // Check if we have enough idle games

                // Remove defunct active games
                lock(_activeGames)
                {
                    _activeGames.RemoveAll(game => game.Status == GameStatus.Kill);
                }

                lock(_idleGames)
                {
                    _idleGames = _activeGames.Where(game => game.Status == GameStatus.Waiting && game.NumClients < 4).ToList<Game>();

                    // Create new ones if there are not enough idle games
                    if (_idleGames.Count < WaitingGames)
                    {
                        Game game = new Game(_gameIdCounter++);
                        Thread gameThread = new Thread(game.Run);
                        gameThread.Start();

                        _activeGames.Add(game);
                        _idleGames.Add(game);
                    }
                }

                int gameNum = _activeGames.Count;
                int idleGames = _idleGames.Count;
                int players = _activeGames.Sum(game => game.NumClients);

                Console.Title = "Active games: " + gameNum + ", waiting games: " + idleGames + ", players: " + players;

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
                _connectedLobbyClients.Add(connection);

                // Setup async data receive
                SocketAsyncEventArgs sArgs = new SocketAsyncEventArgs();
                sArgs.SetBuffer(new byte[512], 0, 512);
                sArgs.Completed += ClientMessageReceived;
                connection.ReceiveAsync(sArgs);
            }
        }

        /// <summary>
        /// Sends list of current games to passed client socket
        /// </summary>
        /// <param name="socket">Socket belonging to client</param>
        private void SendGameList(Socket socket)
        {
            Console.WriteLine("[LIST] Returning game list to " + socket.RemoteEndPoint + "...");

            GameList gameList = new GameList(this._activeGames);
            MemoryStream ms = new MemoryStream();
            gameList.Serialize(ms);
            socket.Send(ms.GetBuffer());
        }

        private void ClientMessageReceived(object sender, SocketAsyncEventArgs e)
        {
            Console.WriteLine("Received client message!");
        }
    }
}
