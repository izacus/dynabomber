using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DynaBomberClient.MainGame;

namespace DynaBomberClient.MainMenu
{
    public class MainMenuState : IGameState
    {
        private Page _page;
        private TextBox _usernameBox;


        public MainMenuState(Page page)
        {
            _page = page;
        }

        private void PrepareCanvas()
        {
            // Press any key text
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
            Canvas.SetTop(usernameText, 262);

            _page.GameArea.Children.Add(usernameText);

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
            Canvas.SetTop(_usernameBox, 260);

            _usernameBox.GotFocus += UsernameBoxGotFocus;

            _page.GameArea.Children.Add(_usernameBox);
        }

        void UsernameBoxGotFocus(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Got focus!");
            _usernameBox.SelectAll();
        }

        private void StartGame(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) 
                return;

            Global.Nickname = _usernameBox.Text.Trim();

            _page.KeyUp -= StartGame;
            _page.ActiveState = new MainGameState(_page);

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

            _page.KeyUp += StartGame;
        }

        public void Deactivate()
        {
            _page.GameArea.Children.Clear();
        }
    }
}
