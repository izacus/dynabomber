using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using DynaBomberClient.Brick;
using DynaBomberClient.GameOver;
using DynaBomberClient.MainGame.Bombs;
using DynaBomberClient.MainGame.Communication;
using DynaBomberClient.MainGame.Communication.ServerMsg;
using DynaBomberClient.MainGame.Players;

namespace DynaBomberClient.MainGame
{
        /// <summary>
    /// Represents possible states of the game
    /// </summary>
    public enum RunStates
    {
        WaitingForMap,
        WaitingForGameStart,
        GameInProgress,
        GameOver,
        GameError
    }

    /// <summary>
    /// Holds information about current game in progress passed from server
    /// </summary>
    public class CurrentGameInformation
    {
        private volatile RunStates _gameState;
        private Map _currentMap = null;

        private Dictionary<PlayerColor, Player> _players;

        private List<Bomb> _bombs;

        private MainGameState _mainState;

        public CurrentGameInformation(MainGameState mainState)
        {
            State = RunStates.WaitingForMap;

            _mainState = mainState;
            _players = new Dictionary<PlayerColor, Player>();
            _bombs = new List<Bomb>();
        }

        public void UpdateStatus(ServerStatusUpdate update)
        {
            // Update players first
            foreach (PlayerInfo player in update.Players)
            {
                // Don't change coordinates of the local player
                if (player.Color == _mainState.LocalPlayer.Color)
                    continue;

                var ply = GetPlayer(player.Color);

                if (ply == null)
                {
                    AddPlayer(player.Color, player.X, player.Y);
                }
                else
                {
                    // Dispatch position change to the UI thread
                    PlayerInfo info = player;

                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                                                                  {
                                                                      ply.X = info.X;
                                                                      ply.Y = info.Y;
                                                                      ply.Direction = info.Direction;
                                                                      ply.Moving = info.Moving;
                                                                  });
                }
            }

            // Check for command
            switch (update.Update)
            {
                case ServerCommand.StartGame:
                    GameStart();
                    break;

                case ServerCommand.PlayerUpdate:
                    // Do nothing, players were already updated
                    break;

                // A new bomb was set, display it
                case ServerCommand.BombSet:

                    AutoResetEvent rst = new AutoResetEvent(false);
                    Bomb bomb = null;

                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                                                                  {
                                                                      bomb = new Bomb(_mainState.GameCanvas, update.X,update.Y);
                                                                      rst.Set();
                                                                  });

                    rst.WaitOne();

                    lock(_bombs)
                    {
                        _bombs.Add(bomb);
                    }

                    Debug.WriteLine("Bomb set on " + bomb.Position);

                    break;

                case ServerCommand.ScrambleControls:
                    _mainState.LocalPlayer.ScrambleControls();
                    RemovePowerup(update.X, update.Y);
                    break;

                case ServerCommand.ClearPowerup:
                    RemovePowerup(update.X, update.Y);
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        public void RemovePowerup(int gridX, int gridY)
        {
            // Remove "brick" with the powerup
            Point absolutePosition = Util.GetAbsoluteCoordinates(new Point(gridX, gridY));

            // Find the powerup brick
            lock(_mainState.Bricks)
            {
                IEnumerable<Brick.Brick> powerup =
                    _mainState.Bricks.Where(brick => brick.Position.X == absolutePosition.X && brick.Position.Y == absolutePosition.Y);
                
                if (powerup.Count() > 0)
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() => ((DestroyableBrick) powerup.First()).Terminate());
                }
            }
        }

        public void ExplodeBomb(BombExplode explosion)
        {
            lock(_bombs)
            {
                // Find bomb
                IEnumerable<Bomb> bombEnum = from bomb in _bombs
                                             where (bomb.Position.X == explosion.X && bomb.Position.Y == explosion.Y)
                                             select bomb;

                Bomb explodingBomb = bombEnum.First();
                explodingBomb.Range = explosion.Range;
                explodingBomb.MapToCheck = _currentMap;

                Deployment.Current.Dispatcher.BeginInvoke(explodingBomb.Explode);

                _bombs.Remove(explodingBomb);
            }

            foreach (BrickPosition destroyedBrickPos in explosion.DestroyedBricks)
            {

                Point absoluteBrickPosition = Util.GetAbsoluteCoordinates(new Point(destroyedBrickPos.X, destroyedBrickPos.Y));

                lock(_mainState.Bricks)
                {
                    IEnumerable<Brick.Brick> foundBrick = _mainState.Bricks.Where(brick => brick.Position.X == absoluteBrickPosition.X && brick.Position.Y == absoluteBrickPosition.Y);

                    if (foundBrick.Count() > 0)
                    {
                        BrickPosition pos = destroyedBrickPos;
                        Deployment.Current.Dispatcher.BeginInvoke(() => ((DestroyableBrick)foundBrick.First()).ShutDown(pos.SpawnedPowerup));
                    }
                }
            }
        }

        public void KillPlayer(PlayerDeath death)
        {
            Player player = GetPlayer(death.PlayerColor);

            Debug.WriteLine("Killing player " + player.Color);

            Deployment.Current.Dispatcher.BeginInvoke(player.ShutDown);
        }

        private void GameStart()
        {
            // Wait for the local player to be created
            while (_mainState.LocalPlayer == null)
            { };

            AutoResetEvent displayUpdated = new AutoResetEvent(false);

            // Update status display
            Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        // Hide status message and display game canvas
                        _mainState.DisplayStatusMessage("");
                        _mainState.GameCanvas.Visibility = Visibility.Visible;
                        displayUpdated.Set();
                    }); 

            displayUpdated.WaitOne();

            State = RunStates.GameInProgress;
            // Start sending player location periodically
            Thread reportThread = new Thread(_mainState.MainGameLoop);
            reportThread.Start();
        }

        public void EndGame(GameOverUpdate gameOver)
        {
            State = RunStates.GameOver;
            // Wait for few seconds
            Thread.Sleep(2000);

            Deployment.Current.Dispatcher.BeginInvoke(() =>
                ((Page) Application.Current.RootVisual).ActiveState = new GameOverState(_mainState.LocalPlayer.Color, gameOver.Winner));
        }

        #region Interface
        public Map Level
        {
            set 
            { 
                if (_currentMap == null)
                {
                    _currentMap = value;

                    lock(_mainState.Bricks)
                    {
                        // Create and set brick objects
                        _mainState.Bricks = _currentMap.CreateBricks(_mainState.GameCanvas);
                    }

                    Debug.WriteLine("Bricks created.");
                }
            }

            get
            {
                return _currentMap;
            }
        }

        public RunStates State
        {
            get
            {
                return _gameState;
            }
            set
            {
                _gameState = value;
                Debug.WriteLine("Switching game state to " + value);
            }
        }

        public Player GetPlayer(PlayerColor color)
        {
            return _players.ContainsKey(color) ? _players[color] : null;
        }

        public void AddPlayer(PlayerColor color, int x, int y)
        {

            Player player = null;
            
            AutoResetEvent reset = new AutoResetEvent(false);

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                player = new Player(_mainState, color, x, y);
                reset.Set();
            });

            reset.WaitOne();

            _players[color] = player;

            // First player is local player
            if (_mainState.LocalPlayer == null)
            {
                _mainState.LocalPlayer = player;
                Deployment.Current.Dispatcher.BeginInvoke(() => _mainState.DisplayPlayerHead(player.Color));
            }

            Debug.WriteLine("Adding player of color " + color);
        }

        public List<Bomb> Bombs
        {
            get { return this._bombs; }
        }

        #endregion
    }
}
