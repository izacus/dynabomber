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

namespace DynaBomberClient.GameLobby
{
    public static class LobbyGraphics
    {
        public static ListBoxItem CreateItem(ListBox gameList, int gameid, string[] players)
        {
            // Create panel to hold player names
            Grid panel = new Grid
                             {
                                 Width = gameList.Width - 20,
                                 Margin = new Thickness(5, 2, 5, 2)
                             };



            // Create textboxes with player names
            for (int i = 0; i < players.Length; i++ )
            {
                TextBlock playerName = new TextBlock
                                           {
                                               Text = players[i],
                                               Margin = new Thickness(2, 0, 2, 0),
                                               Foreground = new SolidColorBrush(Colors.White)
                                           };

                panel.ColumnDefinitions.Add(new ColumnDefinition());

                panel.Children.Add(playerName);
                Grid.SetColumn(playerName, i);
            }


            ListBoxItem item = new ListBoxItem {Content = panel, Tag = gameid};

            return item;
        }
    }
}
