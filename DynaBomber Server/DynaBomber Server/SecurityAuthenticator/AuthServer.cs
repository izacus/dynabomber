using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.IO;

namespace DynaBomber_Server.SecurityAuthenticator
{
    /// <summary>
    /// Listens on port 932 to answer security queries for silverlight clients
    /// </summary>
    class AuthServer
    {
        private const int AuthPort = 943;

        private const string Policy =
             "<?xml version=\"1.0\" encoding =\"utf-8\"?> " +
             "   <access-policy>  " +
             "     <cross-domain-access> " +
             "      <policy> " +
             "        <allow-from> " +
             "          <domain uri=\"*\" /> " +
             "        </allow-from>  "  +
             "        <grant-to>  " +
             "          <socket-resource port=\"4502-4506\" protocol=\"tcp\" /> " +
             "        </grant-to>   " +
             "      </policy>   " +
             "    </cross-domain-access>  " +
             "  </access-policy>";

        private Socket _listeningSocket;


        public AuthServer()
        {
            _listeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _listeningSocket.Bind(new IPEndPoint(IPAddress.Any, AuthPort));
            _listeningSocket.Listen(10);

            // Start listening thread
            Thread responseThread = new Thread(PolicyResponseLoop) {IsBackground = true};
            responseThread.Start();


            Console.WriteLine("[AUTH] Authentication server up and running on port " + AuthPort);
        }

        private void PolicyResponseLoop()
        {
            while(true)
            {
                // Accept incoming authentication request
                Socket connection = _listeningSocket.Accept();

                Console.WriteLine("[AUTH] Client connection received...");

                // Read request data
                byte[] rawReceived = new byte[1024];
                connection.Receive(rawReceived);

                // Convert data to string
                UTF8Encoding encoding = new UTF8Encoding();
                string receivedString = encoding.GetString(rawReceived).Trim('\0');

                // Check if the client is requesting the policy file
                if (receivedString == "<policy-file-request/>")
                {
                    // Send policy file back
                    byte[] policyData = Encoding.UTF8.GetBytes(Policy);
                    connection.Send(policyData);

                    Console.WriteLine("[AUTH] Policy data sent.");
                }
                
                connection.Close();
            }
        }
    }
}
