using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DynaBomber_Server.GameClasses;
using System.Net.Sockets;
using DynaBomber_Server.Interop;
using Player = DynaBomber_Server.GameClasses.Player;

namespace DynaBomber_Server
{
    public enum GameStatus
    {
        Waiting,
        Playing,
        End,
        Kill
    }

    public enum PlayerColors
    {
        Cyan,
        Green,
        Red,
        Blue,
        None
    }

    public enum Powerup
    {
        None = 0,
        AdditionalBomb = 1,
        BombRange = 2,
        ScrambledControls = 3,
        ManualTrigger = 4
    }

    /// <summary>
    /// Represents a single running game
    /// </summary>
    class Game
    {
        private Map _level;
        private List<Client> _clients;
        private List<PlayerColors> _availableColors;

        private List<Bomb> _bombs;

        public Game()
        {
            this.Status = GameStatus.Waiting;
            this._level = new Map(15, 13);

            this._clients = new List<Client>();
            this._bombs = new List<Bomb>();

            this._availableColors = new List<PlayerColors> {PlayerColors.Cyan, PlayerColors.Red, PlayerColors.Green, PlayerColors.Blue};

            Console.WriteLine("New game created...");
        }

        public void AddClient(Socket connection)
        {
            lock(_clients)
            {
                // Create new player for the client
                Player player = new Player(_availableColors[0]);
                _availableColors.RemoveAt(0);

                Client newClient = new Client(connection, player);

                newClient.SendMap(_level);

                // Send new player info to all clients);
                foreach (Client client in _clients)
                {
                    try
                    {
                        client.SendPlayerInfo(player);
                    }
                    catch (SocketException e)
                    {
                        connection.Close();
                        this.Status = GameStatus.Kill;
                    }
                }

                _clients.Add(newClient);
            }
        }

        public void Run()
        {
            while(this.Status != GameStatus.Kill)
            {
                switch (Status)
                {
                    case GameStatus.Waiting:
                        Waiting();
                        break;

                    case GameStatus.Playing:
                        Playing();
                        break;

                    case GameStatus.End:
                        EndGame();
                        break;

                    default:
                        throw new NotImplementedException();
                }

                // Check client status
                if (_clients.Any(client => !client.SocketAvailable()))
                {
                    this.Status = GameStatus.Kill;
                }

                Thread.Sleep(5);
            }

            Console.WriteLine("Game killed.");
        }

        private void EndGame()
        {
            Console.WriteLine("Game over!");

            Player survivingPlayer = null;

            // Get life player
            lock(_clients)
            {
                IEnumerable<Player> livePlayers =
                    (from client in _clients where !client.LocalPlayer.Dead select client.LocalPlayer);

                if (livePlayers.Count() > 0)
                {
                    survivingPlayer = livePlayers.First();
                }
            }

            // Send notification to all players
            lock(_clients)
            {
                // Send gameover to all clients at once
                Thread[] disconnectThreads = new Thread[_clients.Count];
                int i = 0;

                foreach (Client client in _clients)
                {
                    // SendGameOver blocks until client confirms
                    Client cl = client;

                    disconnectThreads[i] = new Thread(() => cl.SendGameOver(new GameOverMessage(survivingPlayer == null ? PlayerColors.None : survivingPlayer.Color)));
                    disconnectThreads[i++].Start();
                }

                // Wait for all clients to receive
                foreach (Thread t in disconnectThreads)
                    t.Join();
            }

            Console.WriteLine("Status notification finished, ending game...");

            // Disconnect all clients
            lock(_clients)
            {
                foreach (Client client in _clients)
                {
                    client.CloseConnection();
                }
            }

            // End game;
            this.Status = GameStatus.Kill;
        }

        private void Playing()
        {
            Player[] players = null;

            lock(_clients)
            {
                players = (from client in _clients select client.LocalPlayer).ToArray();
            }


            // Send status update to all players
            lock(_clients)
            {
                foreach (Client cl in _clients)
                {
                    Client client = cl;
                    ThreadPool.QueueUserWorkItem(o => client.SendStatusUpdate(new GameStatusUpdate(players, Command.PlayerUpdate)));
                }
            }

            // Check for exploded bombs
            CheckBombs();

            // Check for collected powerups
            CheckPowerups();

            // Check if the game has ended
            lock(_clients)
            {
                int livePlayerCount = _clients.Count(client => !client.LocalPlayer.Dead);

                // All other players are dead, end game
                if (livePlayerCount < 2)
                {
                    this.Status = GameStatus.End;
                }
            }
        }

        /// <summary>
        /// Checks if any player has collected a powerup
        /// </summary>
        private void CheckPowerups()
        {
            lock(_clients)
            {
                foreach(Client cl in _clients)
                {
                    Point gridPos = Util.ToGridCoordinates(new Point(cl.LocalPlayer.X, cl.LocalPlayer.Y));

                    Powerup powerup = _level.GetPowerup(gridPos);

                    Player[] players = null;

                    lock (_clients)
                    {
                        players = (from client in _clients select client.LocalPlayer).ToArray();
                    }

                    if (powerup != Powerup.None)
                    {
                        Console.WriteLine((string) ("Player " + cl.LocalPlayer.Color + " collected " + powerup));

                        switch(powerup)
                        {
                            case Powerup.BombRange:
                                cl.LocalPlayer.BombRange++;
                                Console.WriteLine("[" + cl.LocalPlayer.Color + "] Bomb range " + cl.LocalPlayer.BombRange);
                                break;

                            case Powerup.AdditionalBomb:
                                cl.LocalPlayer.BombNumber++;
                                Console.WriteLine("[" + cl.LocalPlayer.Color + "] Number of bombs: " + cl.LocalPlayer.BombNumber);
                                break;

                            case Powerup.ManualTrigger:
                                cl.LocalPlayer.ManualTrigger = true;
                                Console.WriteLine("[" + cl.LocalPlayer.Color + "] Has manual trigger. ");
                                break;

                            case Powerup.ScrambledControls:
                                Console.WriteLine("[" + cl.LocalPlayer.Color + "] Has scrambled controls. ");
                                break;

                            default:
                                Console.WriteLine("[" + cl.LocalPlayer.Color + "] Unimplemented powerup.");
                                break;
                        }

                        _level.SetPowerup(gridPos, Powerup.None);

                        if (powerup == Powerup.ScrambledControls)
                        {
                            cl.SendStatusUpdate(new GameStatusUpdate(players, Command.ScrambleControls, gridPos.X, gridPos.Y));

                            foreach (Client otherCl in _clients)
                            {
                                if (cl == otherCl)
                                    continue;

                                Client ocl = otherCl;
                                ThreadPool.QueueUserWorkItem(o => ocl.SendStatusUpdate(new GameStatusUpdate(players, Command.ClearPowerup, gridPos.X, gridPos.Y)));
                            }
                        }
                        else
                        {
                            foreach (Client otherCl in _clients)
                            {
                                Client ocl = otherCl;
                                ThreadPool.QueueUserWorkItem(o => ocl.SendStatusUpdate(new GameStatusUpdate(players, Command.ClearPowerup, gridPos.X, gridPos.Y)));   
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Checks if any bombs have exploded
        /// </summary>
        private void CheckBombs()
        {
            lock (_bombs)
            {
                List<Bomb> defunctBombs = new List<Bomb>();

                foreach (Bomb bomb in _bombs.Where(bomb => bomb.IsTimeUp()))
                {
                    Point absoluteBombPosition = Util.ToRealCoordinates(bomb.Position);

                    List<Player> players = null;

                    lock(_clients)
                    {
                        players = (from client in _clients select client.LocalPlayer).ToList();
                    }

                    List<Player> deadPlayers = null;

                    // Find and destroy bricks
                    Point[] destroyedBrickPos = _level.GetDestroyedBricksAndPlayers(bomb, players, out deadPlayers);

                    foreach (Player deadPlayer in deadPlayers)
                    {
                        deadPlayer.Dead = true;
                    }

                    // Kill all dead players
                    lock(_clients)
                    {
                        foreach (Client cl in _clients)
                        {
                            Client client = cl;
                            
                            ThreadPool.QueueUserWorkItem(o =>
                                                             {
                                                                 foreach (Player deadPlayer in deadPlayers)
                                                                 {
                                                                     client.SendStatusUpdate(new PlayerDeath(deadPlayer.Color));
                                                                 }
                                                             });
                        }
                    }

                    // Spawn powerups
                    BrickPosition[] destroyedBricks = new BrickPosition[destroyedBrickPos.Length];

                    int i = 0;

                    Random rnd = new Random();

                    foreach (Point pos in destroyedBrickPos)
                    {

                        int powerupNum = rnd.Next(10); // 

                        Powerup powerup;

                        switch (powerupNum)
                        {
                            case 1:
                                powerup = Powerup.AdditionalBomb;
                                break;
                            case 2:
                                powerup = Powerup.ManualTrigger;
                                break;
                            case 3:
                                powerup = Powerup.BombRange;
                                break;
                            case 4:
                                powerup = Powerup.ScrambledControls;
                                break;
                            default:
                                powerup = Powerup.None;
                                break;
                        }

                        _level.SetPowerup(pos, powerup);
                        destroyedBricks[i++] = new BrickPosition(pos, powerup);

                        if (powerup != Powerup.None)
                            Console.WriteLine((string) ("Spawned powerup " + powerup));
                    }

                    lock (_clients)
                    {
                        foreach (Client cl in _clients)
                        {
                            Client client = cl;
                            Bomb bmb = bomb;
                            ThreadPool.QueueUserWorkItem(o => client.SendStatusUpdate(new BombExplosion(absoluteBombPosition.X, absoluteBombPosition.Y, bmb.Range, destroyedBricks)));
                        }
                    }

                    defunctBombs.Add(bomb);
                }

                _bombs.RemoveAll(defunctBombs.Contains);
            }
        }

        private void Waiting()
        {
            int readyClients;

            do
            {
                lock(_clients)
                {
                    // Send map to waiting clients
                    readyClients = _clients.Count(client => client.State == ClientState.WaitingForStart);

                    // Wait for start command
                    _clients.ForEach(client => client.CheckReady());

                    IEnumerable<Client> defunctClients = _clients.Where(client => client.State == ClientState.Defunct);

                    defunctClients.ToList().ForEach(client =>
                                                        {
                                                            _clients.Remove(client);
                                                            _availableColors.Add(client.LocalPlayer.Color);
                                                            Console.WriteLine("Removing defunct " + client.LocalPlayer.Color);
                                                        });
                }

                Thread.Sleep(5);
            } 
            while (_clients.Count < 2 || readyClients != _clients.Count);// 1 for testing

            // Send start command to each client
            Console.WriteLine("Starting game...");

            Player[] players = (from client in _clients select client.LocalPlayer).ToArray();

            // Send start game command to all clients and start receiveing data from them
            foreach (Client cl in _clients)
            {
                cl.SendStatusUpdate(new GameStatusUpdate(players, Command.StartGame));
                cl.SetupDataReceiveLoop(ClientDataReceived);
            }

            this.Status = GameStatus.Playing;
        }

        private void ClientDataReceived(Client client, string data)
        {
            // Don't process input from dead players
            if (client.LocalPlayer.Dead)
                return;

            // Player position update
            if (data.StartsWith("POS"))
            {

                string[] splitData = data.Split(' ');

                int x = Convert.ToInt32(splitData[1]);
                int y = Convert.ToInt32(splitData[2]);

                client.LocalPlayer.X = x;
                client.LocalPlayer.Y = y;

                MovementDirection dir = (MovementDirection)Enum.Parse(typeof (MovementDirection), splitData[3]);
                client.LocalPlayer.Direction = dir;

                bool moving = Boolean.Parse(splitData[4]);
                client.LocalPlayer.Moving = moving;
            }
            else if (data.StartsWith("BMB"))
            {
                SetBomb(client, data);
            }
            // Trigger manual control bombs
            else if (data.StartsWith("TRG"))
            {
                if (!client.LocalPlayer.ManualTrigger)
                    return;

                lock(_bombs)
                {
                    _bombs.ForEach(bomb =>
                                       {
                                           if (bomb.OwnerColor == client.LocalPlayer.Color)
                                               bomb.Trigger();
                                       });
                }
            }
        }

        /// <summary>
        /// Handles setting up a new bomb by a player
        /// </summary>
        /// <param name="client">Client that sent the set bomb command</param>
        /// <param name="receivedMessage">The set bomb command with parameters</param>
        public void SetBomb(Client client, string receivedMessage)
        {
            Bomb bomb = null;

            // Check if player is allowed to set more bombs
            lock (_bombs)
            {
                int bombcount = _bombs.Count(bmb => bmb.OwnerColor == client.LocalPlayer.Color);

                if (bombcount >= client.LocalPlayer.BombNumber)
                {
                    Console.WriteLine("Too many bombs set.");
                    return;
                }



                string[] splitData = receivedMessage.Split(' ');
                int x = Convert.ToInt32(splitData[1]);
                int y = Convert.ToInt32(splitData[2]);

                Point position = Util.ToGridCoordinates(new Point(x, y));

                // Check if there's already bomb on current position

                int bombsHere = _bombs.Count(bmb => bmb.Position == position);

                if (bombsHere > 0)
                {
                    Console.WriteLine("Bomb already exists on this position.");
                    return;
                }


                // Create new bomb
                bomb = new Bomb(position, client.LocalPlayer.ManualTrigger ? 0 : 2000, client.LocalPlayer.BombRange, client.LocalPlayer.Color);


                _bombs.Add(bomb);
            }

            // Notify all clients about the new bomb
            lock (_clients)
            {
                Player[] players = (from cl in _clients select cl.LocalPlayer).ToArray();
                Point absolutePosition = Util.ToRealCoordinates(bomb.Position);

                foreach (Client cl in _clients)
                {
                    cl.SendStatusUpdate(new GameStatusUpdate(players, Command.BombSet, absolutePosition.X, absolutePosition.Y));
                }
            }
        }


        #region Getters and setters

        public GameStatus Status { get; private set; }
        public int NumClients
        {
            get { return _clients.Count; }
        }

        #endregion
    }
}
