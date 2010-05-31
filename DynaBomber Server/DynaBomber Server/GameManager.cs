using System.Collections.Generic;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Net;

namespace DynaBomber_Server
{
    /// <summary>
    /// Manages running games on the server
    /// </summary>
    class GameManager
    {
        private const int WaitingGames = 1;
        private const int ServerPort = 4502;

        private Socket _serverSocket;

        /// <summary>
        /// Holds list of active game objects
        /// </summary>
        private readonly List<Game> _activeGames;
        private List<Game> _idleGames;

        /// <summary>
        /// Constructor
        /// </summary>
        public GameManager()
        {
            _activeGames = new List<Game>();
            _idleGames = new List<Game>();
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
                        Game game = new Game();
                        Thread gameThread = new Thread(new ThreadStart(game.Run));
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

        private void SocketListener()
        {
            // Create the server socket
            _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _serverSocket.Bind(new IPEndPoint(IPAddress.Any, ServerPort));
            _serverSocket.Listen(10);

            while(true)
            {
                Socket connection = _serverSocket.Accept();

                Boolean gameFound = false;

                do
                {
                    lock (_idleGames)
                    {
                        if (_idleGames.Count > 0 && _idleGames[0].NumClients < 4)
                        {
                            // Delegate the connection to an idle game
                            _idleGames[0].AddClient(connection);
                            gameFound = true;
                        }
                    }

                    Thread.Sleep(1);
                } 
                while (!gameFound);

            }
        }
    }
}
