using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using DynaBomberClient.MainMenu;
using DynaBomberClient.Player;

namespace DynaBomberClient.GameOver
{
    public class GameOverState : IGameState
    {
        private PlayerColor _localPlayer;
        private PlayerColor _winner;

        private Storyboard _trophyAnimation;
        private Storyboard _cloudAnimation;

        public GameOverState(PlayerColor local, PlayerColor winner)
        {
            this._localPlayer = local;
            this._winner = winner;
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
            Page page = (Page)Application.Current.RootVisual;
            DisplaySprites(page, _localPlayer, _winner);

            if (_winner != PlayerColor.None)
                DisplayTrophy(page);

            if (_localPlayer != _winner)
                DisplayCloud(page);


            page.KeyUp += ReturnToMenu;
        }

        public void Deactivate()
        {
            _trophyAnimation.Stop();

            if (_cloudAnimation != null)
                _cloudAnimation.Stop();
        }

        private void DisplaySprites(Page page, PlayerColor local, PlayerColor winner)
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
                //page.winRect.Fill = image;
                //page.winRect.Visibility = Visibility.Visible;
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
                //page.loseRect.Fill = image;
                //page.loseRect.Visibility = Visibility.Visible;
            }
        }

        private void DisplayTrophy(Page page)
        {
            ImageBrush images = new ImageBrush
                                    {
                                        Stretch = Stretch.None,
                                        AlignmentX = AlignmentX.Left,
                                        AlignmentY = AlignmentY.Top
                                    };

            images.ImageSource = ResourceHelper.GetBitmap("Graphics/trophy-animation.png");

           // page.trophyRect.Fill = images;

            TranslateTransform animPosition = new TranslateTransform();
            images.Transform = animPosition;

            _trophyAnimation = Util.CreateAnimationSequence(animPosition, 0, 5, true, 200, 50);
            _trophyAnimation.Begin();

           // page.trophyRect.Visibility = Visibility.Visible;
        }

        private void DisplayCloud(Page page)
        {
            ImageBrush images = new ImageBrush
            {
                Stretch = Stretch.None,
                AlignmentX = AlignmentX.Left,
                AlignmentY = AlignmentY.Top
            };

            images.ImageSource = ResourceHelper.GetBitmap("Graphics/cloud-animation.png");

           // page.cloudRect.Fill = images;

            TranslateTransform animPosition = new TranslateTransform();
            images.Transform = animPosition;

            _cloudAnimation = Util.CreateAnimationSequence(animPosition, 0, 5, true, 200, 128);
            _cloudAnimation.Begin();

           // page.cloudRect.Visibility = Visibility.Visible;
        }
    }
}
