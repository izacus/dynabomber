using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using ProtoBuf;

namespace DynaBomberClient.MainGame.Server
{
    public class Remote
    {
        private const int ServerPort = 4502;
        private const int ReceiveBufferSize = 5120;

        private CurrentGameInformation _gameInfo;

        private Socket _socket;
        private MemoryStream _receivedData;

        private AutoResetEvent syncResetEvent = new AutoResetEvent(false);

        public Remote(CurrentGameInformation gameInfo)
        {
            _gameInfo = gameInfo;

            string serverAddress = Application.Current.Host.Source.Host;

            // For testing
            if (serverAddress == "")
                serverAddress = "127.0.0.1";

            // Setup server connection
            DnsEndPoint endPoint = new DnsEndPoint(serverAddress, ServerPort);

            // Establish connection to server
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);


            // Setup async socket
            SocketAsyncEventArgs socketArgs = new SocketAsyncEventArgs();
            socketArgs.UserToken = _socket;
            socketArgs.RemoteEndPoint = endPoint;
            socketArgs.Completed += SocketConnected;

            _socket.ConnectAsync(socketArgs);
        }

        private void SocketConnected(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                Debug.WriteLine("Failed to connect to server!");
                Debug.WriteLine(e.SocketError);

                _gameInfo.State = RunStates.GameError;
                return;
            }

            Debug.WriteLine("Successfully connected to the server...");

            // Update status display
            /*Deployment.Current.Dispatcher.BeginInvoke(
                () => ((Page)Application.Current.RootVisual).statusLabel.Text = "Waiting for map..."); */

            // Reconfigure socket to receive data
            byte[] response = new byte[2048];
            e.SetBuffer(response, 0, response.Length);

            // Switch event handlers for received data
            e.Completed -= SocketConnected;
            e.Completed += SocketDataReceived;

            // Set a 4KB receive buffer
            e.SetBuffer(new byte[ReceiveBufferSize], 0, ReceiveBufferSize);
            Socket socket = (Socket)e.UserToken;

            _receivedData = new MemoryStream();

            socket.ReceiveAsync(e);
        }

        private void SocketDataReceived(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                Debug.WriteLine(e.SocketError);
                return;
            }

            lock(_receivedData)
            {
                _receivedData.Write(e.Buffer, e.Offset, e.BytesTransferred);
            }

            MessageReceived();

            // Prepare socket for another receive session
            Socket socket = (Socket)e.UserToken;

            try
            {
                socket.ReceiveAsync(e);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

        }

        private void MessageReceived()
        {
            lock (_receivedData)
            {
                _receivedData.Seek(0, SeekOrigin.Begin);

                int messageType = _receivedData.ReadByte();
                MessageType type = (MessageType) messageType;

                switch(type)
                {
                    case MessageType.Map:
                        Map map = Serializer.DeserializeWithLengthPrefix<Map>(_receivedData, PrefixStyle.Base128);
                        Debug.WriteLine("Map received.");

                        // Map successfully received, change game state to wait for start
                        _gameInfo.Level = map;
                        _gameInfo.State = RunStates.WaitingForGameStart;

                        // Update status display
                        /*Deployment.Current.Dispatcher.BeginInvoke(
                            () => ((Page)Application.Current.RootVisual).statusLabel.Text = "Waiting for players to be ready...\nYou are NOT ready."); */

                        SendResponse("MAP OK");
                        break;

                    case MessageType.Player:
                        PlayerInfo playerInfo = Serializer.DeserializeWithLengthPrefix<PlayerInfo>(_receivedData, PrefixStyle.Base128);

                        _gameInfo.AddPlayer(playerInfo.Color, playerInfo.X, playerInfo.Y);
                        SendResponse("PI OK\n");

                        Debug.WriteLine("Player info received...");
                        break;

                    case MessageType.StatusUpdate:
                        StatusUpdate update = Serializer.DeserializeWithLengthPrefix<StatusUpdate>(_receivedData,PrefixStyle.Base128);
                        _gameInfo.UpdateStatus(update);
                        break;

                    case MessageType.BombExplosion:
                        BombExplode explosion = Serializer.DeserializeWithLengthPrefix<BombExplode>(_receivedData, PrefixStyle.Base128);
                        _gameInfo.ExplodeBomb(explosion);
                        break;

                    case MessageType.PlayerDeath:
                        PlayerDeath playerDeath = Serializer.DeserializeWithLengthPrefix<PlayerDeath>(_receivedData, PrefixStyle.Base128);
                        _gameInfo.KillPlayer(playerDeath);
                        break;

                    case MessageType.GameOver:
                        GameOverUpdate gameOverUpdate = Serializer.DeserializeWithLengthPrefix<GameOverUpdate>(_receivedData, PrefixStyle.Base128);
                        _gameInfo.EndGame(gameOverUpdate);
                        SendResponseSync("GO OK");
                        break;

                    default:
                        Debug.WriteLine("Unrecognised package received!");
                        break;
                }

                _receivedData = new MemoryStream();
            }
        }

        private void ProcessMessage(String message)
        {
            if (_gameInfo.State == RunStates.GameOver || _gameInfo.State == RunStates.GameError)
                return;

            try
            {
                // Check what we received)
                if (message.Contains("<Map"))
                {
                    // Received map data
                    Map map = (Map)Util.DeserializeXml(message, typeof(Map));
                    Debug.WriteLine("Map deserialized.");

                    // Map successfully received, change game state to wait for start
                    _gameInfo.Level = map;
                    _gameInfo.State = RunStates.WaitingForGameStart;

                    // Update status display
                    /*Deployment.Current.Dispatcher.BeginInvoke(
                        () => ((Page)Application.Current.RootVisual).statusLabel.Text = "Waiting for players to be ready...\nYou are NOT ready."); */

                    SendResponse("MAP OK");
                }
                else if (message.Contains("<StatusUpdate"))
                {
                    StatusUpdate update = (StatusUpdate)Util.DeserializeXml(message, typeof(StatusUpdate));
                    _gameInfo.UpdateStatus(update);
                }
                else if (message.Contains("<Player"))
                {
                    // Received player data, update the player
                    PlayerInfo pinfo = (PlayerInfo)Util.DeserializeXml(message, typeof(PlayerInfo));

                    // Local player info received, update game parateres

                    // Create and add new player in UI thread
                    _gameInfo.AddPlayer(pinfo.Color, pinfo.X, pinfo.Y);
                    SendResponse("PI OK\n");

                    Debug.WriteLine("Player info received...");
                }
                else if (message.Contains("<BombExplode"))
                {
                    BombExplode explosion = (BombExplode)Util.DeserializeXml(message, typeof(BombExplode));
                    _gameInfo.ExplodeBomb(explosion);
                }
                else if (message.Contains("<Death"))
                {
                    PlayerDeath death = (PlayerDeath) Util.DeserializeXml(message, typeof (PlayerDeath));
                    _gameInfo.KillPlayer(death);
                }
                else if (message.Contains("<GameOver"))
                {
                    Debug.WriteLine("Game over!");
                    GameOverUpdate gameOver = (GameOverUpdate) Util.DeserializeXml(message, typeof (GameOverUpdate));
                    _gameInfo.EndGame(gameOver);
                    SendResponseSync("GO OK");
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            catch (InvalidOperationException e)
            {
                Debug.WriteLine("DECODE ERROR, " + e.Message);
                throw;
            }
        }

        private void SendResponseSync(string message)
        {
            if (!_socket.Connected)
            {
                Debug.WriteLine("Socket disconnected when sending sync response!");
                return;
            }

            syncResetEvent.Reset();

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = _socket.RemoteEndPoint;
            args.UserToken = _socket;

            args.Completed += SyncSendCompleted;

            byte[] data = Encoding.UTF8.GetBytes(message);
            args.SetBuffer(data, 0, data.Length);

            _socket.SendAsync(args);

            syncResetEvent.WaitOne();
        }

        private void SyncSendCompleted(object sender, SocketAsyncEventArgs e)
        {
            syncResetEvent.Set();
            ((Socket)e.UserToken).Close();
        }

        private void SendResponse(string response)
        {
            if (!_socket.Connected)
            {
                Debug.WriteLine("Socket disconnected when sending response!");
                return;
            }

            Debug.WriteLine(response);

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = _socket.RemoteEndPoint;
            args.UserToken = _socket;

            byte[] data = Encoding.UTF8.GetBytes(response);
            args.SetBuffer(data, 0, data.Length);

            _socket.SendAsync(args);
        }

        public void SendPlayerLocation(Player.Player player)
        {
            if (!_socket.Connected)
                return;

            // Setup async send and forget about it
            string message = "POS " + player.Position.X + " " + player.Position.Y + " " + player.Direction + " " + player.Moving + "\n";
            
            SendResponse(message);
        }

        public void SendBombSetNotify(Point location)
        {
            string message = "BMB " + location.X + " " + (location.Y - 8) + "\n";
            SendResponse(message);
        }

        public void SendTriggerCommand()
        {
            const string message = "TRG\n";
            SendResponse(message);
        }

        public void SendStartRequest()
        {
            const string message = "STRT\n";
            SendResponse(message);

            // Update status display
      /*      Deployment.Current.Dispatcher.BeginInvoke(
                () => ((Page)Application.Current.RootVisual).statusLabel.Text = "Waiting for players to be ready...\nYou are ready."); */
        }

        public bool SocketConnected()
        {
            return _socket.Connected;
        }
    }
}