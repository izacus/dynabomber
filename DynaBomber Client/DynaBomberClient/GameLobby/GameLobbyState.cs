#region

using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DynaBomberClient.Communication.ClientMsg;
using DynaBomberClient.Communication.ServerMsg;
using DynaBomberClient.MainGame;
using ProtoBuf;

#endregion

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
            Page page = (Page) Application.Current.RootVisual;
            page.GameArea.Children.Clear();
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

            eargs.Completed += ServerResponseReceived;

            socket.ReceiveAsync(eargs);
        }

        private void ServerResponseReceived(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                Debug.WriteLine("Error receiving game list!");
                return;
            }

            MemoryStream ms = new MemoryStream(e.Buffer, e.Offset, e.BytesTransferred);
            ServerMessageTypes messageType = (ServerMessageTypes) ms.ReadByte();


            switch (messageType)
            {
                case ServerMessageTypes.GameList:
                    GameListMessage gameList = Serializer.DeserializeWithLengthPrefix<GameListMessage>(ms, PrefixStyle.Base128);
                    Deployment.Current.Dispatcher.BeginInvoke(() => UpdateGameList(gameList));
                    break;

                case ServerMessageTypes.SimpleResponse:
                    ServerResponse response = Serializer.DeserializeWithLengthPrefix<ServerResponse>(ms, PrefixStyle.Base128);

                    // Received successful join response, switch to game
                    if (response.Value == ServerResponse.Response.JoinOk)
                    {
                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                                                                      {
                                                                          Page page =
                                                                              (Page) Application.Current.RootVisual;
                                                                          page.ActiveState = new MainGameState(_socket);
                                                                      });
                    }

                    // !!
                    // RETURN from function without setting async hook
                    return;
            }
           
            // Prepare for new receive
            ((Socket) e.UserToken).ReceiveAsync(e);
        }

        private void UpdateGameList(GameListMessage message)
        {
            lock(_gameList)
            {
                // Remember current selected item
                int selectedItem = _gameList.SelectedIndex;

                // Recreate list
                _gameList.Items.Clear();
                foreach (GameInfo game in message.Games)
                {
                    _gameList.Items.Add(LobbyGraphics.CreateItem(_gameList, game.ID, game.Players));
                    ;
                }

                if (selectedItem == -1)
                {
                    _gameList.SelectedIndex = 0;
                }
                else
                {
                    _gameList.SelectedIndex = selectedItem;
                }
            }
        }

        private void JoinGame(object sender, RoutedEventArgs e)
        {
            // Send request to join the game
            SocketAsyncEventArgs sArgs = new SocketAsyncEventArgs();
            sArgs.RemoteEndPoint = _socket.RemoteEndPoint;
            sArgs.UserToken = _socket;

            int gameID;

            lock(_gameList)
            {
                gameID = (int)((ListBoxItem) _gameList.SelectedItem).Tag;
            }
            ClientJoinGameRequest joinRequest = new ClientJoinGameRequest(gameID, Global.Nickname);

            MemoryStream ms = new MemoryStream();
            joinRequest.Serialize(ms);
            byte[] data = ms.GetBuffer();
            
            sArgs.SetBuffer(data, 0, data.Length);
            _socket.SendAsync(sArgs);
        }
    }
}
