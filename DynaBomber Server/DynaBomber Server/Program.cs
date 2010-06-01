using System;
using System.Reflection;
using DynaBomber_Server.SecurityAuthenticator;

namespace DynaBomber_Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("DynaBomber server " + Assembly.GetExecutingAssembly().GetName().Version.ToString() + " starting up...");

            // Start authentication server
            AuthServer authServer = new AuthServer();

            GameManager manager = new GameManager();
            manager.Run();
        }
    }
}
