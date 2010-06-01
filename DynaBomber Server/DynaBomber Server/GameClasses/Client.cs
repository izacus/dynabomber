using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using DynaBomber_Server.Interop.ClientMsg;
using DynaBomber_Server.Interop.ServerMsg;
using ProtoBuf;

namespace DynaBomber_Server.GameClasses
{
    public enum ClientState
    {
        WaitingForMap,
        WaitingForReady,
        WaitingForStart,
        Defunct
    }

    /// <summary>
    /// Helper class for async socket operation
    /// </summary>
    class AsyncReceive
    {
        public const int BufferSize = 64;
        private byte[] _receiveBuffer;

        private StringBuilder _receivedData;


        public AsyncReceive()
        {
            _receiveBuffer = new byte[BufferSize];
            _receivedData = new StringBuilder();
        }

        /// <summary>
        /// Receive buffer
        /// </summary>
        public byte[] Buffer
        {
            get
            {
                return _receiveBuffer;
            }
        }

        /// <summary>
        /// Buffer includes a received message from client
        /// </summary>
        public Boolean HasMessage
        {
            get
            {
                return _receivedData.ToString().IndexOf('\n') > -1;
            }
        }

        /// <summary>
        /// Extract a single message from the buffer
        /// </summary>
        /// <returns>Received message</returns>
        public string GetMessage()
        {
            if (!HasMessage)
            {
                return null;
            }

            string allData = _receivedData.ToString();
            int endOfMessage = allData.IndexOf('\n');

            _receivedData.Remove(0, endOfMessage + 1);

            return allData.Substring(0, endOfMessage);            
        }

        public void AppendString(string rec)
        {
            _receivedData.Append(rec);
        }

        public string ReceivedData()
        {
            return _receivedData.ToString();
        }
    }

    public class Client
    {
        private readonly Socket _socket;            // Socket connection to the client
        private readonly Player _player;            // Local player

        // Socket data reading
        private Boolean _receivingData;             // Notes if async data receive hook should be running
        private Boolean _receivingDataEnd;          // Notes if async data receive has been unhooked

        // Received message delegate
        public delegate void DataReceived(Client client, IClientUpdate update);

        // Received message callback
        private DataReceived _dataReceived;

        public Client(Socket connection, Player player)
        {
            _socket = connection;
            _socket.Blocking = true;
            _player = player;
            _receivingData = false;

            State = ClientState.WaitingForMap;

            Console.WriteLine("New client from " + connection.RemoteEndPoint + " with color " + _player.Color);
        }

        /// <summary>
        /// Sends level data to the client with local player type
        /// </summary>
        /// <param name="map"></param>
        public void SendMap(Map map)
        {
            Console.WriteLine("Sending map data...");
            // Serialize the map
            byte[] mapData = Util.SerializeUpdate(map);
            byte[] buffer = new byte[512];

            try
            {
                _socket.Send(mapData);
                // Wait for confirmation
                _socket.Receive(buffer);

            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);

                State = ClientState.Defunct;
                return;
            }

            IClientUpdate response = GetUpdate(buffer, 0, 512);

            if (response is ClientStatusUpdate && ((ClientStatusUpdate)response).Update == ClientUpdate.MapOk)
            {
                // Send player information
                SendPlayerInfo(_player);

                // Map was sent, change state
                State = ClientState.WaitingForReady;
            }
            else
            {
                Console.WriteLine("Invalid map response from client!");
                State = ClientState.Defunct;
            }
        }

        public void CheckReady()
        {
            try
            {
                _socket.Blocking = false;
                _socket.Send(new byte[1], 0, 0);
                _socket.Blocking = true;

                if (!_socket.Connected)
                {
                    State = ClientState.Defunct;
                    return;
                }
            }
            catch (SocketException e)
            {
                if (!e.NativeErrorCode.Equals(10035))
                {
                    State = ClientState.Defunct;
                    return;
                }
            }

            if (State != ClientState.WaitingForReady)
                return;


            if (_socket.Available > 0)
            {
                byte[] buffer = new byte[512];
                _socket.Receive(buffer);

                IClientUpdate update = GetUpdate(buffer, 0, buffer.Length);

                if (update is ClientStatusUpdate && ((ClientStatusUpdate)update).Update == ClientUpdate.Ready)
                {
                    State = ClientState.WaitingForStart;
                    _socket.Blocking = true;

                    Console.WriteLine("Received ready from " + LocalPlayer.Color);
                }
            }
        }

        /// <summary>
        /// Send information about a player to the client
        /// </summary>
        /// <param name="player"></param>
        public void SendPlayerInfo(Player player)
        {
            byte[] buffer = new byte[512];

            try
            {
                byte[] playerData = Util.SerializeUpdate(player);
                _socket.Send(playerData);

                // Wait for confirmation
                _socket.Receive(buffer);
            }
            catch (SocketException ex)
            {
                Console.WriteLine("Error while sending player info: " + ex.Message);
            }

            IClientUpdate response = GetUpdate(buffer, 0, buffer.Length);

            if (response is ClientStatusUpdate && ((ClientStatusUpdate)response).Update == ClientUpdate.PlayerInfoOk)
            {
                Console.WriteLine("Player info sent OK");
            }
        }

        /// <summary>
        /// Serializes and sends a status update to the client
        /// </summary>
        /// <param name="update"></param>
        public void SendStatusUpdate(IServerUpdate update)
        {
            try
            {
                byte[] updateData = Util.SerializeUpdate(update);
                _socket.BeginSend(updateData, 0, updateData.Length, SocketFlags.None, null, null);
            }
            catch (SocketException ex)
            {
                Console.WriteLine("Error " + ex.Message);
            }
        }

        public void SendGameOver(GameOverMessage message)
        {
            // Disable async receive
            StopDataReceiveLoop();

            while(!_receivingDataEnd)
                Thread.Sleep(0);

            byte[] gameoverData = Util.SerializeUpdate(message);
            _socket.Send(gameoverData);

            IClientUpdate response = null;

            while(!(response is ClientStatusUpdate && (response as ClientStatusUpdate).Update == ClientUpdate.GameOverOk))
            {
                byte[] buffer = new byte[512];

                try
                {
                    _socket.Receive(buffer);
                }
                catch (SocketException e)
                {
                    break;
                }
                
                response = GetUpdate(buffer, 0, buffer.Length);
            }

            Console.WriteLine("Game over received OK");
        }

        /// <summary>
        /// Checks if the socket is still available and connected
        /// 
        /// It does that by sending 0 bytes to update the connected property
        /// </summary>
        /// <returns></returns>
        public Boolean SocketAvailable()
        {
            Boolean socketBlocking = _socket.Blocking;

            try
            {
                byte[] tmp = new byte[1];
                _socket.Blocking = false;
                _socket.Send(tmp, 0, 0);
            }
            catch(SocketException ex)
            {
                _socket.Blocking = socketBlocking;

                if (ex.NativeErrorCode.Equals(10035))   // Socket would block but it's still connected
                {
                    return true;
                }

                Console.WriteLine("Socket error: " + ex.Message);

                return false;
            }
            catch(ObjectDisposedException ex)
            {
                Console.WriteLine("Socket disposed!");
                return false;
            }

            _socket.Blocking = socketBlocking;

            return _socket.Connected;
        }

        /// <summary>
        /// Starts data receive loop for the client
        /// </summary>
        public void SetupDataReceiveLoop(DataReceived callback)
        {
            _receivingData = true;
            _dataReceived = callback;

            AsyncReceive recObject = new AsyncReceive();

            try
            {
                _socket.BeginReceive(recObject.Buffer, 0, AsyncReceive.BufferSize, SocketFlags.None,
                                     new AsyncCallback(SocketReceive), recObject);
            }
            catch (SocketException e)
            {
                Console.WriteLine("Socket error:" + e.Message);
            }
        }

        public void StopDataReceiveLoop()
        {
            _receivingData = false;
            _receivingDataEnd = false;
        }

        /// <summary>
        /// Socket receive callback
        /// </summary>
        /// <param name="ar"></param>
        private void SocketReceive(IAsyncResult ar)
        {
            if (!_receivingData)
            {
                _socket.EndReceive(ar);

                Console.WriteLine("Unhooked async receive");
                _receivingDataEnd = true;

                return;
            }

            AsyncReceive recObject = (AsyncReceive) ar.AsyncState;

            try
            {
                int bytesRead = _socket.EndReceive(ar);

                // There's more data in the socket
                if (bytesRead > 0)
                {
                    IClientUpdate update = GetUpdate(recObject.Buffer, 0, recObject.Buffer.Length);
                     _dataReceived(this, update);

                    // Get rest of the data
                    _socket.BeginReceive(recObject.Buffer, 0, AsyncReceive.BufferSize, SocketFlags.None, new AsyncCallback(SocketReceive), recObject);
                }
                else
                {
                    // Prepare for new receive
                    recObject = new AsyncReceive();
                    _socket.BeginReceive(recObject.Buffer, 0, AsyncReceive.BufferSize, SocketFlags.None, new AsyncCallback(SocketReceive), recObject);

                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine("Receive error: " + ex.Message);
                return;
            }
            catch (ObjectDisposedException ex)
            {
                Console.WriteLine("Receive error: " + ex.Message);
                return;
            }
        }

        /// <summary>
        /// Closes connection to the client
        /// </summary>
        public void CloseConnection()
        {
            _socket.Close();
        }

        private IClientUpdate GetUpdate(byte[] buffer, int offset, int length)
        {
            MemoryStream ms = new MemoryStream(buffer, offset, length);

            ClientMessageTypes msgType = (ClientMessageTypes) ms.ReadByte();

            switch (msgType)
            {
                case ClientMessageTypes.StatusUpdate:
                    ClientStatusUpdate statusUpdate = Serializer.DeserializeWithLengthPrefix<ClientStatusUpdate>(ms,PrefixStyle.Base128);
                    return statusUpdate;
                    
                case ClientMessageTypes.PositionUpdate:
                    ClientPositionUpdate positionUpdate = Serializer.DeserializeWithLengthPrefix<ClientPositionUpdate>(ms, PrefixStyle.Base128);
                    return positionUpdate;

                case ClientMessageTypes.BombSet:
                    ClientBombSet bombSetUpdate = Serializer.DeserializeWithLengthPrefix<ClientBombSet>(ms, PrefixStyle.Base128);
                    return bombSetUpdate;
            }

            return null;
        }

        #region Interface
        public ClientState State { get; private set; }

        public Player LocalPlayer
        {
            get
            {
                return _player;
            }
        }

        #endregion
    }
}
