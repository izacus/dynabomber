using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace DynaBomberClient
{
    public static class Global
    {
        public static string Nickname = "Fonzelj";
        public static string ServerAddress = null;
        public static int ServerPort = 4502;


        public static string GetServerAddress()
        {
            string serverAddress = "";

            if (Application.Current.IsRunningOutOfBrowser)
                serverAddress = Global.ServerAddress;
            else
                serverAddress = Application.Current.Host.Source.Host;

            // For debug purposes
            // TODO: remove
            if (serverAddress == "")
                serverAddress = "localhost";

            return serverAddress;
        }
    }
}
