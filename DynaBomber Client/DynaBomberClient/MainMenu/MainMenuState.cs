using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DynaBomberClient.GameLobby;
using DynaBomberClient.MainGame;

namespace DynaBomberClient.MainMenu
{
    public class MainMenuState : IGameState
    {
        private Page _page;
        private TextBox _usernameBox;
        private TextBox _serverAddress;


        public MainMenuState(Page page)
        {
            _page = page;
        }

        private void PrepareCanvas()
        {
            // Game title text
            TextBlock dynaBomberText = new TextBlock
                                           {
                                               Text = "DYNABOMBER",
                                               Width = _page.Width,
                                               TextAlignment = TextAlignment.Center,
                                               FontWeight = FontWeights.Bold,
                                               FontSize = 56
                                           };

            // Text gradient
            dynaBomberText.Foreground = new LinearGradientBrush
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

            Canvas.SetLeft(dynaBomberText, 0);
            Canvas.SetTop(dynaBomberText, 60);

            _page.GameArea.Children.Add(dynaBomberText);

            // Nickname
            TextBlock usernameText = new TextBlock
            {
                Text = "Nickname:",
                TextAlignment = TextAlignment.Right,
                FontSize = 20,
                Width = 150,
                Height = 40,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Colors.White)
            };

            Canvas.SetLeft(usernameText, 100);
            Canvas.SetTop(usernameText, 232);

            _page.GameArea.Children.Add(usernameText);



            _usernameBox = new TextBox
                                      {
                                          AcceptsReturn = false,
                                          Background = new SolidColorBrush(Colors.Black),
                                          Foreground = new SolidColorBrush(Colors.White),
                                          FontSize = 20,
                                          Width = 250,
                                          Height = 40,
                                          Text = Global.Nickname,
                                          VerticalAlignment = VerticalAlignment.Center
                                      };

            Canvas.SetLeft(_usernameBox, 260);
            Canvas.SetTop(_usernameBox, 230);

            _usernameBox.GotFocus += TextBoxGotFocus;

            _page.GameArea.Children.Add(_usernameBox);


            Button connectButton = new Button
                                       {
                                           Background = new SolidColorBrush(Colors.Black),
                                           Foreground = new SolidColorBrush(Colors.Black),
                                           FontSize = 14,
                                           Content = "Connect...",
                                           Width = 120,
                                       };

            Canvas.SetLeft(connectButton, 270);
            Canvas.SetTop(connectButton, 360);
            connectButton.Click += new RoutedEventHandler(ConnectButtonClick);

            _page.GameArea.Children.Add(connectButton);

            if (Application.Current.IsRunningOutOfBrowser)
            {
                // Press any key text
                TextBlock serverText = new TextBlock
                {
                    Text = "Server address:",
                    TextAlignment = TextAlignment.Right,
                    FontSize = 20,
                    Width = 190,
                    Height = 40,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = new SolidColorBrush(Colors.White)
                };

                Canvas.SetLeft(serverText, 60);
                Canvas.SetTop(serverText, 292);

                _page.GameArea.Children.Add(serverText);

                _serverAddress = new TextBox
                {
                    AcceptsReturn = false,
                    Background = new SolidColorBrush(Colors.Black),
                    Foreground = new SolidColorBrush(Colors.White),
                    FontSize = 20,
                    Width = 250,
                    Height = 40,
                    Text = "localhost",
                    VerticalAlignment = VerticalAlignment.Center
                };

                Canvas.SetLeft(_serverAddress, 260);
                Canvas.SetTop(_serverAddress, 290);

                _usernameBox.GotFocus += TextBoxGotFocus;

                _page.GameArea.Children.Add(_serverAddress);
            }
            else if (Application.Current.InstallState == InstallState.NotInstalled)
            {
                Button installButton = new Button
                                            {
                                                Background = new SolidColorBrush(Colors.Black),
                                                Foreground = new SolidColorBrush(Colors.Black),
                                                FontSize = 14,
                                                Content = "Install",
                                                Width = 100
                                            };

                Canvas.SetLeft(installButton, 530);
                Canvas.SetTop(installButton, 10);

                installButton.Click += InstallButtonClick;

                _page.GameArea.Children.Add(installButton);
            }
            
        }

        void ConnectButtonClick(object sender, RoutedEventArgs e)
        {
            StartGame();
        }

        void InstallButtonClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Install();
        }

        void TextBoxGotFocus(object sender, RoutedEventArgs e)
        {

            TextBox box = sender as TextBox;

            if (box == null)
                return;

            box.SelectAll();
        }

        private void KeyPressed(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter || Application.Current.InstallState == InstallState.Installing)
                return;

            StartGame();
        }

        private void StartGame()
        {
            if (Application.Current.IsRunningOutOfBrowser && !System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                MessageBox.Show("Cannot connect if you are not connected to the internet.");
                return;
            }

            Global.Nickname = _usernameBox.Text.Trim();

            if (Application.Current.IsRunningOutOfBrowser)
                Global.ServerAddress = _serverAddress.Text.Trim();

            _page.ActiveState = new GameLobbyState(_page);

            Debug.WriteLine("Starting game with username " + Global.Nickname);
        }

        public void EnterFrame(double dt)
        {
            // Nothing TBD
        }

        public void Activate()
        {
            // Prepare required display items
            PrepareCanvas();

            _page.KeyUp += KeyPressed;
        }

        public void Deactivate()
        {
            _page.KeyUp -= KeyPressed;
            _page.GameArea.Children.Clear();
        }
    }
}
