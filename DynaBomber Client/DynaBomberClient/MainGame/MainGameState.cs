using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using DynaBomberClient.Brick;
using DynaBomberClient.Keyboard;
using DynaBomberClient.MainGame.Bombs;
using DynaBomberClient.MainGame.Communication;
using DynaBomberClient.MainGame.Players;
using DynaBomberClient.MainMenu;

namespace DynaBomberClient.MainGame
{
    public class MainGameState : IGameState
    {
        // Visual related
        private Page _page;
        private TextBlock _statusText;
        private Canvas _gameCanvas;
        private Rectangle _statusHead;

        // Class handles server access
        private Server _server;

        // Information about current game in progress
        private CurrentGameInformation _gameInfo;

        private Player _localPlayer = null;
        private List<Brick.Brick> _bricks;

        public MainGameState(Page page)
        {
            // Prepare datastructures
            _bricks = new List<Brick.Brick>();
            _page = page;
        }

        private void PrepareGraphics(Canvas mainCanvas)
        {
            // Player head rect
            _statusHead = new Rectangle
                              {
                                  Width = 44,
                                  Height = 46,
                                  Visibility = Visibility.Collapsed
                              };

            Canvas.SetLeft(_statusHead, 290);
            Canvas.SetTop(_statusHead, 130);
            mainCanvas.Children.Add(_statusHead);

            // Prepare status text display
            _statusText = new TextBlock
                              {
                                  TextAlignment = TextAlignment.Center,
                                  Width = mainCanvas.Width,
                                  FontSize = 30,
                                  Foreground = new SolidColorBrush(Colors.White)
                              };

            Canvas.SetLeft(_statusText, 0);
            Canvas.SetTop(_statusText, 200);

            mainCanvas.Children.Add(_statusText);

            // Prepare game canvas
            _gameCanvas = new Canvas
                              {
                                  Background = new SolidColorBrush(Colors.Black),
                                  Width = 640,
                                  Height = 480,
                                  Visibility = Visibility.Collapsed
                              };

            Canvas.SetLeft(_gameCanvas, 0);
            Canvas.SetTop(_gameCanvas, 0);

            mainCanvas.Children.Add(_gameCanvas);

            // Load level image
            Image levelImage = new Image
                                   {
                                       Width = 640,
                                       Height = 480
                                   };

            Canvas.SetLeft(levelImage, 0);
            Canvas.SetTop(levelImage, 0);

            levelImage.Source = ResourceHelper.GetBitmap("Graphics/MapEmpty.jpg");

            _gameCanvas.Children.Add(levelImage);
        }

        public void EnterFrame(double dt)
        {
            if (_localPlayer == null)
                return;

            _localPlayer.Display();

            if (_gameInfo.State == RunStates.GameInProgress)
            {
                CheckCollision(_localPlayer, _bricks, _gameInfo.Bombs);
            }
        }

        public void Activate()
        {
            // Prepare game information
            _gameInfo = new CurrentGameInformation(this);
            PrepareGraphics(_page.GameArea);
            KeyHandler.Instance.StartupKeyHandler(_page);

            // Prepare remote connection
            DisplayStatusMessage("Connecting...");

            _server = new Server(this, _gameInfo);
        }

        public void Deactivate()
        {
            _gameInfo.State = RunStates.GameOver;

            KeyHandler.Instance.Shutdown();

            Page page = (Page) Application.Current.RootVisual;

            // Remove all created sprites
            page.GameArea.Children.Clear();
        }

        private void CheckCollision(Player player, List<Brick.Brick> bricksList,List<Bomb> bombsList)
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
                _server.SendPlayerLocation(_localPlayer);

                lock(_bricks)
                {
                    List<Brick.Brick> destroyableBricks = (from brick in _bricks
                                                     where brick is DestroyableBrick
                                                     select brick).ToList();

                    foreach (DestroyableBrick brick in
                             destroyableBricks.Cast<DestroyableBrick>().Where(brick => brick.ToRemove))
                    {
                        _bricks.Remove(brick);
                    }
                }

                if (_gameInfo.State != RunStates.GameOver && !_server.SocketConnected())
                    _gameInfo.State = RunStates.GameError;

                Thread.Sleep(10);
            }

            // Disconnect from server
            Debug.WriteLine("Game over, disconnecting...");

            // Handle error
            if (_gameInfo.State == RunStates.GameError)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    _page.ActiveState = new MainMenuState(_page);
                });
            }
        }

        public void DisplayStatusMessage(string message)
        {
            _statusText.Text = message;
        }

        public void DisplayPlayerHead(PlayerColor color)
        {
            if (color == PlayerColor.None)
                _statusHead.Visibility = Visibility.Collapsed;

            ImageBrush headImg = new ImageBrush
                                     {
                                         AlignmentX = AlignmentX.Left,
                                         AlignmentY = AlignmentY.Top,
                                         Stretch = Stretch.None
                                     };

            headImg.ImageSource = ResourceHelper.GetBitmap("Graphics/Player/head-" + color.ToString().ToLower() + ".png");
            _statusHead.Fill = headImg;

            _statusHead.Visibility = Visibility.Visible;
        }

        #region Interface

        public Canvas GameCanvas
        {
            get { return _gameCanvas; }
        }

        public Server Server
        {
            get { return _server; }
        }

        public Player LocalPlayer
        {
            get { return _localPlayer; }
            set { _localPlayer = value; }
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
