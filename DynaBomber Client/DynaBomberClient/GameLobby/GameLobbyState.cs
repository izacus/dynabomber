using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using DynaBomberClient.Communication.ClientMsg;
using DynaBomberClient.Communication.ServerMsg;
using ProtoBuf;

namespace DynaBomberClient.GameLobby
{
    public class GameLobbyState : IGameState
    {
        private Canvas _gameCanvas;
        private ListBox _gameList;

        private Socket _socket;

        public GameLobbyState(Page page)
        {
            _gameCanvas = page.GameArea;

            GameUpdater();
        }


        public void EnterFrame(double dt)
        {
            // Nothing TBD
        }

        public void Activate()
        {
            StackPanel layoutPanel = new StackPanel
                                         {
                                             Orientation = Orientation.Vertical
                                         };


            TextBlock gameListText = new TextBlock
            {
                Text = "Game list",
                Width = _gameCanvas.Width,
                TextAlignment = TextAlignment.Center,
                FontWeight = FontWeights.Bold,
                FontSize = 34
            };

            layoutPanel.Children.Add(gameListText);

            // Text gradient
            gameListText.Foreground = new LinearGradientBrush
            {
                StartPoint = new Point(0.5, 0),
                EndPoint = new Point(0.5, 1),
                GradientStops = new GradientStopCollection
                                {
                                    new GradientStop { Color = Colors.Yellow, Offset = 0},
                                    new GradientStop { Color = Colors.Orange, Offset = 1},
                                    new GradientStop { Color = Colors.Red, Offset = 2}
                                }
            };


            _gameList = new ListBox
                              {
                                  Width = _gameCanvas.Width - 20,
                                  Height = _gameCanvas.Height - 100,
                                  Margin = new Thickness(10, 10, 10, 10),
                                  Background = new SolidColorBrush(Colors.Black),
                                  BorderBrush = null
                              };


            layoutPanel.Children.Add(_gameList);

            Button joinButton = new Button
            {
                //Background = new SolidColorBrush(Colors.Black),
                Foreground = new SolidColorBrush(Colors.Black),
                FontSize = 14,
                Content = "Join",
                Width = 100
            };

            joinButton.Click += JoinGame;

            layoutPanel.Children.Add(joinButton);

            _gameCanvas.Children.Add(layoutPanel);
        }

        public void Deactivate()
        {
            
        }

        public void GameUpdater()
        {
            // Connect to server
            // Setup server connection
            DnsEndPoint endPoint = new DnsEndPoint(Global.GetServerAddress(), Global.ServerPort);

            // Establish connection to server
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            SocketAsyncEventArgs eargs = new SocketAsyncEventArgs
                                             {
                                                 UserToken = _socket,
                                                 RemoteEndPoint = endPoint
                                             };

            eargs.Completed += UpdaterConnected;

            _socket.ConnectAsync(eargs);
        }

        private void UpdaterConnected(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                Debug.WriteLine("Failed to connect to server.");
                return;
            }

            // Send request for game list
            Socket socket = (Socket) e.UserToken;


            SocketAsyncEventArgs eargs = new SocketAsyncEventArgs();
            eargs.RemoteEndPoint = socket.RemoteEndPoint;
            eargs.UserToken = socket;
            eargs.SetBuffer(new byte[512], 0, 512);

            eargs.Completed += GameListReceived;

            socket.ReceiveAsync(eargs);
        }

        private void GameListReceived(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                Debug.WriteLine("Error receiving game list!");
                return;
            }

            MemoryStream ms = new MemoryStream(e.Buffer, e.Offset, e.BytesTransferred);

            // Received game list
            if (ms.ReadByte() == (byte)ServerMessageTypes.GameList)
            {
                GameListMessage gameList = Serializer.DeserializeWithLengthPrefix<GameListMessage>(ms, PrefixStyle.Base128);

                Debug.WriteLine("Got game list!!");

                foreach (GameInfo game in gameList.Games)
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() => _gameList.Items.Add(LobbyGraphics.CreateItem(_gameList, game.ID, game.Players)));
                }     
            }
           
            // Prepare for new receive
            ((Socket) e.UserToken).ReceiveAsync(e);
        }

        private void JoinGame(object sender, RoutedEventArgs e)
        {
            // Send request to join the game
            SocketAsyncEventArgs sArgs = new SocketAsyncEventArgs();
            sArgs.RemoteEndPoint = _socket.RemoteEndPoint;
            sArgs.UserToken = _socket;


            int gameID = (int)((ListBoxItem) _gameList.SelectedItem).Tag;

            ClientJoinGameRequest joinRequest = new ClientJoinGameRequest(gameID, Global.Nickname);

            MemoryStream ms = new MemoryStream();
            joinRequest.Serialize(ms);
            byte[] data = ms.GetBuffer();
            
            sArgs.SetBuffer(data, 0, data.Length);
            _socket.SendAsync(sArgs);
        }
    }
}
