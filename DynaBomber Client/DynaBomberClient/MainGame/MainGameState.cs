using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using DynaBomberClient.Brick;
using DynaBomberClient.Keyboard;
using DynaBomberClient.MainGame.Server;
using DynaBomberClient.MainMenu;

namespace DynaBomberClient.MainGame
{
    public class MainGameState : IGameState
    {
        // Class handles server access
        private Remote _remote;

        // Information about current game in progress
        private CurrentGameInformation _gameInfo;
        private Canvas _gameCanvas;

        private Player.Player _localPlayer = null;
        private List<Brick.Brick> _bricks;
        private List<Bomb.Bomb> _bombs;

        public MainGameState(Canvas gameCanvas)
        {
            // Set visibility on required elements
            Page page = (Page)Application.Current.RootVisual;
            page.statusLabel.Text = "Connecting...";
            page.statusLabel.Visibility = Visibility.Visible;
            page.GameArea.Visibility = Visibility.Collapsed;

            KeyHandler.Instance.StartupKeyHandler(page);

            // Prepare datastructures
            _bricks = new List<Brick.Brick>();
            _bombs = new List<Bomb.Bomb>();
            

            // Store current canvas
            _gameCanvas = gameCanvas;

            // Prepare game information
            _gameInfo = new CurrentGameInformation(this);

            // Prepare remote connection
            _remote = new Remote(_gameInfo);
        }

        public void EnterFrame(double dt)
        {
            if (_localPlayer == null)
                return;

            _localPlayer.Display();

            CheckCollision(_localPlayer, _bricks,_bombs);
        }

        public void Deactivate()
        {
            _gameInfo.State = RunStates.GameOver;

            KeyHandler.Instance.Shutdown();

            Page page = (Page) Application.Current.RootVisual;

            // Remove all created sprites
            List<Rectangle> spriteRects = page.GameArea.Children.OfType<Rectangle>().ToList();

            foreach (var spriteRect in spriteRects)
            {
                page.GameArea.Children.Remove(spriteRect);
            }

            page.statusLabel.Visibility = Visibility.Collapsed;
            page.GameArea.Visibility = Visibility.Collapsed;
            page.headRect.Visibility = Visibility.Collapsed;
        }

        private void CheckCollision(Player.Player player, List<Brick.Brick> bricksList)
        {
            
            lock(bricksList)
            {
                
                foreach (Brick.Brick brick in bricksList)
                {
                    if (brick == null)
                        bricksList.Remove(brick);
                    
                    else
                    player.Collide(brick);
                }
            }
        }

        private void CheckCollision(Player.Player player, List<Brick.Brick> bricksList,List<Bomb.Bomb> bombsList)
        {

            lock (bricksList)
            {

                foreach (Brick.Brick brick in bricksList)
                {
                    if (brick == null)
                        bricksList.Remove(brick);

                    else
                        player.Collide(brick);
                }
            }

            lock (bombsList)
            {
                foreach (var foo in bombsList)
                {
                    player.Collide(foo);
                }
            }
        }

        /// <summary>
        /// Periodically reports current player location to the server as long
        /// as the player is alive
        /// </summary>
        public void MainGameLoop()
        {
            while (_gameInfo.State == RunStates.GameInProgress)
            {
                _remote.SendPlayerLocation(_localPlayer);

                lock(_bricks)
                {
                    List<Brick.Brick> destroyableBricks = (from brick in _bricks
                                                     where brick is DestroyableBrick
                                                     select brick).ToList();

                    foreach (DestroyableBrick brick in
                             destroyableBricks.Cast<DestroyableBrick>().Where(brick => brick.ToRemove))
                    {
                        _bricks.Remove(brick);
                        Point gridBrick = Util.GetRelativeCoordinates(brick.Position);

                        //_gameInfo.Level.ClearBrick((int)gridBrick.X, (int)gridBrick.Y);
                    }
                }

                if (_gameInfo.State != RunStates.GameOver && !_remote.SocketConnected())
                    _gameInfo.State = RunStates.GameError;

                Thread.Sleep(5);
            }

            // Disconnect from server
            Debug.WriteLine("Game over, disconnecting...");

            // Handle error
            if (_gameInfo.State == RunStates.GameError)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    ((Page)Application.Current.RootVisual).ActiveState =
                          new MainMenuState((Page)Application.Current.RootVisual);
                });
            }
        }

        public void SetBombsRef(List<Bomb.Bomb> bombsRef)
        {
            _bombs = bombsRef;
        }

        #region Interface

        public Canvas GameCanvas
        {
            get { return _gameCanvas; }
        }

        public Remote Server
        {
            get { return _remote; }
        }

        public Player.Player LocalPlayer
        {
            get { return _localPlayer; }
            set { _localPlayer = value; }
        }

        public List<Bomb.Bomb> Bombs
        {
            get { return _bombs; }
            set { _bombs = value; }
        }

        public List<Brick.Brick> Bricks
        {
            get { return _bricks; }
            set { _bricks = value; }
        }

        public RunStates State
        {
            get { return _gameInfo.State; }
        }

        #endregion
    }
}
