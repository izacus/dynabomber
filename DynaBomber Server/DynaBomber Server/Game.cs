using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using DynaBomber_Server.GameClasses;
using DynaBomber_Server.Interop.ClientMsg;
using DynaBomber_Server.Interop.ServerMsg;

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
    public class Game
    {
        private readonly Map _level;
        private readonly List<Client> _clients;
        private readonly List<PlayerColors> _availableColors;

        private readonly List<Bomb> _bombs;

        public Game(int id)
        {
            this.ID = id;

            Status = GameStatus.Waiting;
            _level = new Map(15, 13);

            _clients = new List<Client>();
            _bombs = new List<Bomb>();

            _availableColors = new List<PlayerColors> {PlayerColors.Cyan, PlayerColors.Red, PlayerColors.Green, PlayerColors.Blue};

            Console.WriteLine("New game created...");
        }

        /// <summary>
        /// Adds new client to the waiting game
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="playerName"></param>
        public void AddClient(Socket connection, string playerName)
        {
            if (this.Status != GameStatus.Waiting)
                return;

            lock(_clients)
            {
                // Create new player for the client
                Player player = new Player(_availableColors[0]);
                _availableColors.RemoveAt(0);

                // Send map data to the client
                Client newClient = new Client(connection, player, playerName);
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
                        Status = GameStatus.Kill;
                    }
                }

                _clients.Add(newClient);
            }
        }

        /// <summary>
        /// Main game thread loop
        /// </summary>
        public void Run()
        {
            while(Status != GameStatus.Kill)
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
                    Status = GameStatus.Kill;
                }

                Thread.Sleep(5);
            }

            Console.WriteLine("Game killed.");
        }

        /// <summary>
        /// Ends the running game and sends game over notifications
        /// </summary>
        private void EndGame()
        {
            Console.WriteLine("Game over!");

            Player survivingPlayer = null;

            // Get living player
            lock(_clients)
            {
                IEnumerable<Player> livePlayers =
                    (from client in _clients where !client.LocalPlayer.Dead select client.LocalPlayer);

                if (livePlayers.Count() > 0)
                {
                    survivingPlayer = livePlayers.First();
                }

                // Send gameover to all clients at once
                Thread[] disconnectThreads = new Thread[_clients.Count];
                int i = 0;

                foreach (Client client in _clients)
                {
                    // SendGameOver blocks until client confirms
                    Client cl = client;

                    disconnectThreads[i] = new Thread(() => cl.SendGameOver(new ServerGameOver(survivingPlayer == null ? PlayerColors.None : survivingPlayer.Color)));
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
            Status = GameStatus.Kill;
        }

        /// <summary>
        /// Playing game state handler
        /// </summary>
        private void Playing()
        {
            Player[] players = null;

            lock(_clients)
            {
                players = (from client in _clients select client.LocalPlayer).ToArray();
            
                foreach (Client cl in _clients)
                {
                    Client client = cl;
                    ThreadPool.QueueUserWorkItem(o => client.SendStatusUpdate(new ServerGameStatusUpdate(players, Command.PlayerUpdate)));
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
                    Status = GameStatus.End;
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
                            cl.SendStatusUpdate(new ServerGameStatusUpdate(players, Command.ScrambleControls, gridPos.X, gridPos.Y));

                            foreach (Client otherCl in _clients)
                            {
                                if (cl == otherCl)
                                    continue;

                                Client ocl = otherCl;
                                ThreadPool.QueueUserWorkItem(o => ocl.SendStatusUpdate(new ServerGameStatusUpdate(players, Command.ClearPowerup, gridPos.X, gridPos.Y)));
                            }
                        }
                        else
                        {
                            foreach (Client otherCl in _clients)
                            {
                                Client ocl = otherCl;
                                ThreadPool.QueueUserWorkItem(o => ocl.SendStatusUpdate(new ServerGameStatusUpdate(players, Command.ClearPowerup, gridPos.X, gridPos.Y)));   
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
                lock(_clients)
                {
                    List<Bomb> defunctBombs = new List<Bomb>();

                    foreach (Bomb bomb in _bombs.Where(bomb => bomb.IsTimeUp()))
                    {
                        Point absoluteBombPosition = Util.ToRealCoordinates(bomb.Position);

                        List<Player> players = null;

                        players = (from client in _clients where !client.LocalPlayer.Dead select client.LocalPlayer).ToList();
                        List<Player> deadPlayers = null;

                        // Find and destroy bricks
                        Point[] destroyedBrickPos = _level.GetDestroyedBricksAndPlayers(bomb, players, out deadPlayers);

                        foreach (Player deadPlayer in deadPlayers)
                        {
                            deadPlayer.Dead = true;
                        }

                        // Spawn powerups
                        BrickPosition[] destroyedBricks = new BrickPosition[destroyedBrickPos.Length];

                        int i = 0;

                        Random rnd = new Random();

                        foreach (Point pos in destroyedBrickPos)
                        {

                            int powerupNum = rnd.Next(10);

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

                        foreach (Client cl in _clients)
                        {
                            Client client = cl;
                            Bomb bmb = bomb;
                            ThreadPool.QueueUserWorkItem(o => client.SendStatusUpdate(new ServerBombExplosion(absoluteBombPosition.X, absoluteBombPosition.Y, bmb.Range, destroyedBricks)));

                            ThreadPool.QueueUserWorkItem(o =>
                            {
                                foreach (Player deadPlayer in deadPlayers)
                                {
                                    client.SendStatusUpdate(new ServerPlayerDeath(deadPlayer.Color));
                                }
                            });
                        }
                        
                        defunctBombs.Add(bomb);
                    }

                    _bombs.RemoveAll(defunctBombs.Contains);
                }

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
                cl.SendStatusUpdate(new ServerGameStatusUpdate(players, Command.StartGame));
                cl.SetupDataReceiveLoop(ClientDataReceived);
            }

            Status = GameStatus.Playing;
        }

        private void ClientDataReceived(Client client, IClientUpdate update)
        {
            // Don't process input from dead players
            if (client.LocalPlayer.Dead)
                return;

            if (update is ClientPositionUpdate)
            {
                ClientPositionUpdate posUpdate = (ClientPositionUpdate) update;

                client.LocalPlayer.X = posUpdate.X;
                client.LocalPlayer.Y = posUpdate.Y;
                client.LocalPlayer.Direction = posUpdate.Direction;
                client.LocalPlayer.Moving = posUpdate.Moving;
            }
            else if (update is ClientBombSet)
            {
                SetBomb(client, (ClientBombSet)update);
            }
            else if (update is ClientStatusUpdate)
            {
                ClientStatusUpdate statusUpdate = (ClientStatusUpdate) update;
                
                if (statusUpdate.Update == ClientUpdate.BombTrigger && client.LocalPlayer.ManualTrigger)
                {
                    lock (_bombs)
                    {
                        _bombs.ForEach(bomb =>
                        {
                            if (bomb.OwnerColor == client.LocalPlayer.Color)
                                bomb.Trigger();
                        });
                    }
                }

            }
        }

        /// <summary>
        /// Handles setting up a new bomb by a player
        /// </summary>
        /// <param name="client">Client that sent the set bomb command</param>
        /// <param name="receivedMessage">The set bomb command with parameters</param>
        public void SetBomb(Client client, ClientBombSet update)
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


                Point position = Util.ToGridCoordinates(new Point(update.X, update.Y));

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
                    cl.SendStatusUpdate(new ServerGameStatusUpdate(players, Command.BombSet, absolutePosition.X, absolutePosition.Y));
                }
            }
        }


        #region Getters and setters
        public int ID { get; set; }

        public GameStatus Status { get; private set; }
        public int NumClients
        {
            get { return _clients.Count; }
        }

        public List<string> ClientNames
        {
            get
            {
                List<string> clientNames = _clients.Select(client => client.Name).ToList();

                for (int i = 0; i < (4 - _clients.Count); i++)
                    clientNames.Add("Empty");

                return clientNames;
            }
        }

        #endregion
    }
}
