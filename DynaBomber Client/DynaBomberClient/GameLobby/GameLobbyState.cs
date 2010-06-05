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
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            SocketAsyncEventArgs eargs = new SocketAsyncEventArgs
                                             {
                                                 UserToken = socket,
                                                 RemoteEndPoint = endPoint
                                             };

            eargs.Completed += UpdaterConnected;

            socket.ConnectAsync(eargs);
        }

        private void UpdaterConnected(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                Debug.WriteLine("Failed to connect to server.");
                return;
            }

            // Send request for game list
            Socket socket = (Socket)e.UserToken;
            

            SocketAsyncEventArgs eargs = new SocketAsyncEventArgs();
            eargs.RemoteEndPoint = socket.RemoteEndPoint;
            eargs.UserToken = socket;

            eargs.Completed += new EventHandler<SocketAsyncEventArgs>(UpdaterRequestSent);

            // Send single byte request for game list
            byte[] buffer = new byte[1];
            buffer[0] = (byte)ClientMessageTypes.GetGameList;
            eargs.SetBuffer(buffer, 0, buffer.Length);
            socket.SendAsync(eargs);
        }

        private void UpdaterRequestSent(object sender, SocketAsyncEventArgs e)
        {
            // Wait for game list response
            if (e.SocketError != SocketError.Success)
            {
                Debug.WriteLine("Error while sending game list request " + e.SocketError);
                return;
            }

            e.Completed -= UpdaterRequestSent;
            e.Completed += GameListReceived;

            e.SetBuffer(new byte[512], 0, 512);

            ((Socket) e.UserToken).ReceiveAsync(e);
        }

        private void GameListReceived(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                Debug.WriteLine("Error receiving game list!");
                return;
            }

            MemoryStream ms = new MemoryStream(e.Buffer, e.Offset, e.BytesTransferred);

            // Received data is not game list
            if (ms.ReadByte() != (byte)ServerMessageTypes.GameList)
            {
                Debug.WriteLine("Received garbage from server.");
                return;
            }

            GameListMessage gameList = Serializer.DeserializeWithLengthPrefix<GameListMessage>(ms, PrefixStyle.Base128);

            Debug.WriteLine("Got game list!!");
        }
    }
}
