using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using DynaBomberClient.Communication.ClientMsg;
using DynaBomberClient.Communication.ServerMsg;
using DynaBomberClient.MainGame.Players;
using ProtoBuf;

namespace DynaBomberClient.MainGame
{
    public class Server
    {
        private const int ReceiveBufferSize = 5120;

        private readonly MainGameState _mainState;
        private readonly CurrentGameInformation _gameInfo;

        private readonly Socket _socket;

        // Received socket data buffer
        private MemoryStream _receivedData;

        public Server(MainGameState mainState, CurrentGameInformation gameInfo, Socket serverSocket)
        {
            _mainState = mainState;
            _gameInfo = gameInfo;

            // Establish connection to server
            _socket = serverSocket;

            // Update status display
            Deployment.Current.Dispatcher.BeginInvoke(() => _mainState.DisplayStatusMessage("Waiting for map..."));

            SocketAsyncEventArgs e = new SocketAsyncEventArgs();

            // Reconfigure socket to receive data
            byte[] response = new byte[2048];
            e.SetBuffer(response, 0, response.Length);

            // Switch event handlers for received data
            e.Completed += SocketDataReceived;

            // Set a 4KB receive buffer
            e.SetBuffer(new byte[ReceiveBufferSize], 0, ReceiveBufferSize);
            e.UserToken = _socket;

            _receivedData = new MemoryStream();

            _socket.ReceiveAsync(e);
        }

        /// <summary>
        /// Callback for received socket data
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// Handles received message from the received data memorystream
        /// </summary>
        private void MessageReceived()
        {
            lock (_receivedData)
            {
                _receivedData.Seek(0, SeekOrigin.Begin);

                int messageType = _receivedData.ReadByte();
                ServerMessageTypes type = (ServerMessageTypes) messageType;

                switch(type)
                {
                    case ServerMessageTypes.Map:
                        Map map = Serializer.DeserializeWithLengthPrefix<Map>(_receivedData, PrefixStyle.Base128);
                        Debug.WriteLine("Map received.");

                        // Map successfully received, change game state to wait for start
                        _gameInfo.Level = map;
                        _gameInfo.State = RunStates.WaitingForGameStart;

                        // Update status display
                        Deployment.Current.Dispatcher.BeginInvoke(() => _mainState.DisplayStatusMessage("Waiting for players to be ready...\nYou are NOT ready."));

                        SendResponse(new ClientStatusUpdate(ClientUpdate.MapOk));
                        break;

                    case ServerMessageTypes.Player:
                        PlayerInfo playerInfo = Serializer.DeserializeWithLengthPrefix<PlayerInfo>(_receivedData, PrefixStyle.Base128);

                        _gameInfo.AddPlayer(playerInfo.Color, playerInfo.X, playerInfo.Y);
                        SendResponse(new ClientStatusUpdate(ClientUpdate.PlayerInfoOk));

                        Debug.WriteLine("Player info received...");
                        break;

                    case ServerMessageTypes.StatusUpdate:
                        ServerStatusUpdate update = Serializer.DeserializeWithLengthPrefix<ServerStatusUpdate>(_receivedData,PrefixStyle.Base128);
                        _gameInfo.UpdateStatus(update);
                        break;

                    case ServerMessageTypes.BombExplosion:
                        BombExplode explosion = Serializer.DeserializeWithLengthPrefix<BombExplode>(_receivedData, PrefixStyle.Base128);
                        _gameInfo.ExplodeBomb(explosion);
                        break;

                    case ServerMessageTypes.PlayerDeath:
                        PlayerDeath playerDeath = Serializer.DeserializeWithLengthPrefix<PlayerDeath>(_receivedData, PrefixStyle.Base128);
                        _gameInfo.KillPlayer(playerDeath);
                        break;

                    case ServerMessageTypes.GameOver:
                        GameOverUpdate gameOverUpdate = Serializer.DeserializeWithLengthPrefix<GameOverUpdate>(_receivedData, PrefixStyle.Base128);
                        _gameInfo.EndGame(gameOverUpdate);
                        SendResponse(new ClientStatusUpdate(ClientUpdate.GameOverOk));
                        break;

                    default:
                        Debug.WriteLine("Unrecognised package received!");
                        break;
                }

                _receivedData = new MemoryStream();
            }
        }

        /// <summary>
        /// Sends a response to server asynchronosly
        /// There are no successful send guarantees
        /// </summary>
        /// <param name="response"></param>
        private void SendResponse(IClientMessage response)
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

            MemoryStream ms = new MemoryStream();
            response.Serialize(ms);
            byte[] data = ms.GetBuffer();
            args.SetBuffer(data, 0, data.Length);

            _socket.SendAsync(args);
        }

        /// <summary>
        /// Sends a client position update message to the server for local player
        /// </summary>
        /// <param name="player"></param>
        public void SendPlayerLocation(Player player)
        {
            if (!_socket.Connected)
                return;
            
            ClientPositionUpdate update = new ClientPositionUpdate((int)player.Position.X, (int)player.Position.Y, player.Direction, player.Moving);
            SendResponse(update);
        }

        /// <summary>
        /// Sends notification of bomb set attempt to the server
        /// </summary>
        /// <param name="location">Position in absolute X/Y coordinates</param>
        public void SendBombSetNotify(Point location)
        {
            ClientBombSet update = new ClientBombSet((int)location.X, (int)(location.Y - 8));
            SendResponse(update);
        }

        /// <summary>
        /// Notifies server of player trying to trigger his bombs
        /// </summary>
        public void SendTriggerCommand()
        {
            ClientStatusUpdate update = new ClientStatusUpdate(ClientUpdate.BombTrigger);
            SendResponse(update);
        }

        /// <summary>
        /// Sends a player ready notification to server
        /// </summary>
        public void SendStartRequest()
        {
            ClientStatusUpdate update = new ClientStatusUpdate(ClientUpdate.Ready);
            SendResponse(update);

            // Update status display
            Deployment.Current.Dispatcher.BeginInvoke(() => _mainState.DisplayStatusMessage("Waiting for players to be ready...\nYou are ready."));
        }

        /// <summary>
        /// Current server socket connection status
        /// </summary>
        public bool SocketConnected
        {
            get
            {
                return _socket.Connected;   
            }
        }
    }
}