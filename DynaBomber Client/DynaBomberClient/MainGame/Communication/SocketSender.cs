using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace DynaBomberClient.MainGame.Communication
{
    /// <summary>
    /// Handles sendng of socket data for it to be sent in right order over async socket
    /// </summary>
    public class SocketSender
    {
        private Socket _socket;

        /// <summary>
        /// Denotes if socket is currently busy sending data
        /// </summary>
        private Boolean _sendInProgress;

        // Send lock
        private readonly object _sendLock = new object();

        /// <summary>
        /// Data waiting to be sent to client
        /// </summary>
        private readonly Queue<MemoryStream> _waitingData;

        public SocketSender(Socket asyncSocket)
        {
            _socket = asyncSocket;
            _sendInProgress = false;
            _waitingData = new Queue<MemoryStream>();
        }

        public void SendData(MemoryStream data)
        {
            lock(_sendLock)
            {
                _waitingData.Enqueue(data);

                if (!_sendInProgress)
                    SetupSend();
            }

        }

        /// <summary>
        /// Sends new payload from waiting data
        /// WARNING: Does not ensure locking!
        /// </summary>
        private void SetupSend()
        {
            if (_waitingData.Count == 0)
                return;

            SocketAsyncEventArgs e = new SocketAsyncEventArgs();
            e.RemoteEndPoint = _socket.RemoteEndPoint;
            e.UserToken = _socket;

            // Get data from queue
            byte[] data = _waitingData.Dequeue().GetBuffer();
            e.SetBuffer(data, 0, data.Length);
            e.Completed += SendComplete;

            // Update status variable
            _sendInProgress = true;
            
            _socket.SendAsync(e);
        }

        private void SendComplete(object sender, SocketAsyncEventArgs e)
        {
            // Prevent handle leak
            e.Completed -= SendComplete;

            lock(_sendLock)
            {
                _sendInProgress = false;

                // Send next waiting packet if ready
                if (_waitingData.Count > 0)
                {
                    SetupSend();
                }
            }
        }
    }
}
