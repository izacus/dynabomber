﻿using System;
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
using DynaBomberClient.MainGame.Players;
using DynaBomberClient.MainGame.Server;
using DynaBomberClient.MainMenu;

namespace DynaBomberClient.MainGame
{
    public class MainGameState : IGameState
    {
        // Visual related
        private Page _page;
        private TextBlock _statusText;

        // Class handles server access
        private Remote _remote;

        // Information about current game in progress
        private CurrentGameInformation _gameInfo;
        private Canvas _mainCanvas;
        private Canvas _gameCanvas;

        private Player _localPlayer = null;
        private List<Brick.Brick> _bricks;

        public MainGameState(Page page)
        {
            // Prepare datastructures
            _bricks = new List<Brick.Brick>();
            _page = page;
            _mainCanvas = page.GameArea;
        }

        private void PrepareGraphics(Canvas mainCanvas)
        {
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

            _remote = new Remote(this, _gameInfo);
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
                    _page.ActiveState = new MainMenuState(_page);
                });
            }
        }

        public void DisplayStatusMessage(string message)
        {
            _statusText.Text = message;
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
