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

        public MainMenuState(Page page)
        {
            _page = page;
        }

        private void PrepareCanvas()
        {
            // Press any key text
            TextBlock menuText = new TextBlock
                                    {
                                        Text = "Press any key to connect...",
                                        TextAlignment = TextAlignment.Center,
                                        FontSize = 20,
                                        Width = _page.Width,
                                        Foreground = new SolidColorBrush(Colors.White)
                                    };

            Canvas.SetLeft(menuText, 0);
            Canvas.SetTop(menuText, 260);

            _page.GameArea.Children.Add(menuText);

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
        }

        private void StartGame(object sender, KeyEventArgs e)
        {
            _page.KeyUp -= StartGame;
            _page.ActiveState = new MainGameState(_page);
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
