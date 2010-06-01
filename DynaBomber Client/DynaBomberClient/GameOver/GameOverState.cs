using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using DynaBomberClient.MainGame.Players;
using DynaBomberClient.MainMenu;

namespace DynaBomberClient.GameOver
{
    public class GameOverState : IGameState
    {
        // Graphics
        private Canvas _mainCanvas;
        private Rectangle _winRect;
        private Rectangle _loseRect;
        private Rectangle _trophyRect;
        private Rectangle _cloudRect;

        private PlayerColor _localPlayer;
        private PlayerColor _winner;

        private Storyboard _trophyAnimation;
        private Storyboard _cloudAnimation;

        public GameOverState(PlayerColor local, PlayerColor winner)
        {
            _mainCanvas = ((Page) Application.Current.RootVisual).GameArea;

            _localPlayer = local;
            _winner = winner;
        }


        private void ReturnToMenu(object sender, KeyEventArgs e)
        {
            Page page = (Page)Application.Current.RootVisual;
            page.KeyUp -= ReturnToMenu;

            page.ActiveState = new MainMenuState(page);
        }

        public void EnterFrame(double dt)
        {
            // Nothing TBD
        }

        public void Activate()
        {
            PrepareGraphics();

            DisplaySprites(_localPlayer, _winner);

            if (_winner != PlayerColor.None)
                DisplayTrophy();

            if (_localPlayer != _winner)
                DisplayCloud();

            Page page = (Page) Application.Current.RootVisual;
            page.KeyUp += ReturnToMenu;
        }

        private void PrepareGraphics()
        {
            // Game over text
            TextBlock gameOver = new TextBlock
                                     {
                                         Text = "GAME OVER",
                                         Width = 640,
                                         TextAlignment = TextAlignment.Center,
                                         FontWeight = FontWeights.Bold,
                                         FontSize = 56
                                     };

            gameOver.Foreground = new LinearGradientBrush
                                    {
                                        StartPoint = new Point(0.5, 0),
                                        EndPoint = new Point(0.5, 1),
                                        GradientStops = new GradientStopCollection
                                        {
                                            new GradientStop { Color = Colors.Orange, Offset = 0},
                                            new GradientStop { Color = Colors.Red, Offset = 1},
                                            new GradientStop { Color = Colors.Red, Offset = 2}
                                        }
                                    };

            Canvas.SetLeft(gameOver, 0);
            Canvas.SetTop(gameOver, 60);
            _mainCanvas.Children.Add(gameOver);

            // Winner rectangle
            _winRect = new Rectangle
                           {
                               Height = 63,
                               Width = 40,
                               Visibility = Visibility.Collapsed
                           };
            Canvas.SetLeft(_winRect, 220);
            Canvas.SetTop(_winRect, 300);
            _mainCanvas.Children.Add(_winRect);

            // Trophy animation rectangle
            _trophyRect = new Rectangle
                              {
                                  Width = 50,
                                  Height = 192,
                                  Visibility = Visibility.Collapsed
                              };
            Canvas.SetLeft(_trophyRect, 182);
            Canvas.SetTop(_trophyRect, 160);
            _mainCanvas.Children.Add(_trophyRect);

            // Loser display rectangle
            _loseRect = new Rectangle
                            {
                                Height = 63,
                                Width = 40,
                                Visibility = Visibility.Collapsed
                            };

            Canvas.SetLeft(_loseRect, 365);
            Canvas.SetTop(_loseRect, 300);
            _mainCanvas.Children.Add(_loseRect);

            // Cloud above loser
            _cloudRect = new Rectangle
                             {
                                 Width = 128,
                                 Height = 78,
                                 Visibility = Visibility.Collapsed
                             };

            Canvas.SetLeft(_cloudRect, 385);
            Canvas.SetTop(_cloudRect, 220);
            _mainCanvas.Children.Add(_cloudRect);
        }

        public void Deactivate()
        {
            _mainCanvas.Children.Clear();
        }

        private void DisplaySprites(PlayerColor local, PlayerColor winner)
        {
            ImageBrush image = null;

            if (winner != PlayerColor.None)
            {
                // Winner
                image = new ImageBrush
                {
                    Stretch = Stretch.None,
                    AlignmentX = AlignmentX.Left,
                    AlignmentY = AlignmentY.Top
                };

                image.ImageSource = ResourceHelper.GetBitmap("Graphics/Player/win-" + winner.ToString().ToLower() + ".png");
                _winRect.Fill = image;
                _winRect.Visibility = Visibility.Visible;
            }

            // Loser
            if (local != winner)
            {
                image = new ImageBrush
                {
                    Stretch = Stretch.None,
                    AlignmentX = AlignmentX.Left,
                    AlignmentY = AlignmentY.Top
                };

                image.ImageSource = ResourceHelper.GetBitmap("Graphics/Player/lose-" + local.ToString().ToLower() + ".png");
                _loseRect.Fill = image;
                _loseRect.Visibility = Visibility.Visible;
            }
        }

        private void DisplayTrophy()
        {
            ImageBrush images = new ImageBrush
                                    {
                                        Stretch = Stretch.None,
                                        AlignmentX = AlignmentX.Left,
                                        AlignmentY = AlignmentY.Top
                                    };

            images.ImageSource = ResourceHelper.GetBitmap("Graphics/trophy-animation.png");

            _trophyRect.Fill = images;

            TranslateTransform animPosition = new TranslateTransform();
            images.Transform = animPosition;

            _trophyAnimation = Util.CreateAnimationSequence(animPosition, 0, 5, true, 200, 50);
            _trophyAnimation.Begin();

           _trophyRect.Visibility = Visibility.Visible;
        }

        private void DisplayCloud()
        {
            ImageBrush images = new ImageBrush
            {
                Stretch = Stretch.None,
                AlignmentX = AlignmentX.Left,
                AlignmentY = AlignmentY.Top
            };

            images.ImageSource = ResourceHelper.GetBitmap("Graphics/cloud-animation.png");

            _cloudRect.Fill = images;

            TranslateTransform animPosition = new TranslateTransform();
            images.Transform = animPosition;

            _cloudAnimation = Util.CreateAnimationSequence(animPosition, 0, 5, true, 200, 128);
            _cloudAnimation.Begin();

            _cloudRect.Visibility = Visibility.Visible;
        }
    }
}
