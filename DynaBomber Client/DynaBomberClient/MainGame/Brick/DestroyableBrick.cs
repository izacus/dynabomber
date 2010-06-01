using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using DynaBomberClient.MainGame.Communication;
using DynaBomberClient.MainGame.Communication.ServerMsg;

namespace DynaBomberClient.Brick
{

    public class DestroyableBrick : Brick
    {
        private const string ExplosionFilename = "BrickExplosion1";
        private const int AnimationTickMs = 160;

        private Canvas _gameCanvas;
        private Map _level;

        private Powerup _whichPowerup;
        private Storyboard _currentAnimation;
        private string _powerupFilename;

        private bool _destroyed = false;
        private bool _toRemove = false;

        public bool Destroyed
        {
            get { return _destroyed; }            
        }

        public bool ToRemove
        {
            get { return _toRemove; }
        }

        public Powerup WhichPowerup
        {
            get { return _whichPowerup; }
            private set { _whichPowerup = value; }
        }

        public DestroyableBrick(Map level, Canvas gameCanvas, int x, int y)
        {
            this._level = level;
            this._gameCanvas = gameCanvas;

            inUse = true;
            position.X = x;
            position.Y = y;
            BrickSprite(gameCanvas);
        }

        public override void Collide(Rectangle other)
        {
        }

        public override Rectangle GetRectangle()
        {
            return _spriteRect;
        }


        private void BrickSprite(Canvas parent)
        {
            _spriteRect = new Rectangle
            {
                Height = BrickWidth,
                Width = BrickHeight,
                //Stroke = new SolidColorBrush(Colors.Yellow)
            };

            ImageBrush spriteSheet = new ImageBrush
            {
                Stretch = Stretch.None,
                AlignmentX = AlignmentX.Left,
                AlignmentY = AlignmentY.Top
            };

            // Prepare transform for sprite animation
            TranslateTransform spriteSheetPosition = new TranslateTransform();
            spriteSheet.Transform = spriteSheetPosition;

            // Load sprite sheet image
            spriteSheet.ImageSource = ResourceHelper.GetBitmap("Graphics/Brick/" + ExplosionFilename + ".png");
            _spriteRect.Fill = spriteSheet;

            // Create sprite animations
            CreateAnimations(spriteSheetPosition);

            parent.Children.Add(_spriteRect);
            Canvas.SetLeft(_spriteRect, position.X);
            Canvas.SetTop(_spriteRect, position.Y);
        }
        
        public void ShutDown(Powerup spawnedPowerup)
        {
            _whichPowerup = spawnedPowerup;
            _powerupFilename = spawnedPowerup.ToString();

            _currentAnimation.Seek(TimeSpan.Zero);
            _currentAnimation.RepeatBehavior = new RepeatBehavior(1);

            
            if(spawnedPowerup == Powerup.None)
            {
                _currentAnimation.Completed += Dispose;
            }
            else
            {
                _currentAnimation.Completed += DisplayPowerup;
            }

            _currentAnimation.Begin();
            
        }

        private void ClearMap()
        {
            Point absolutePos = Util.GetRelativeCoordinates(this.Position);
            _level.ClearBrick((int)absolutePos.X, (int)absolutePos.Y);
        }

         private void DisplayPowerup(object sender, EventArgs e)
         {
            ClearMap();

             _gameCanvas.Children.Remove(_spriteRect);

            _spriteRect = new Rectangle
                              {
                                  Height = BrickWidth,
                                  Width = BrickHeight,
                                  StrokeThickness = 0
                              };

             ImageBrush spriteSheet = new ImageBrush
            {
                Stretch = Stretch.None,
                AlignmentX = AlignmentX.Left,
                AlignmentY = AlignmentY.Top
            };

            // Prepare transform for sprite animation
            TranslateTransform spriteSheetPosition = new TranslateTransform();
            spriteSheet.Transform = spriteSheetPosition;

            // Load sprite sheet image
            spriteSheet.ImageSource = ResourceHelper.GetBitmap("Graphics/Powerups/" + _powerupFilename + ".png");
            _spriteRect.Fill = spriteSheet;

            Canvas.SetLeft(_spriteRect, position.X);
            Canvas.SetTop(_spriteRect, position.Y);
            Canvas.SetZIndex(_spriteRect, 0);

             _gameCanvas.Children.Add(_spriteRect);
            _destroyed = true;
        }
        
        private void Dispose(object sender, EventArgs e)
        {
            ClearMap();

            _gameCanvas.Children.Remove(_spriteRect);
            inUse = false;
            _toRemove = true;
        }

        public void Terminate()
        {
            Dispose(new object(),new EventArgs());
        }

        private Storyboard CreateAnimationSequence(TranslateTransform translateTransform)
        {
            var animation = new DoubleAnimationUsingKeyFrames();

            for (int i = 1; i <= 6; i++)
            {
                TimeSpan animSpan = new TimeSpan(0, 0, 0, 0, i * AnimationTickMs);

                // Add keyframe to animation
                animation.KeyFrames.Add(new DiscreteDoubleKeyFrame
                {
                    Value = -BrickHeight * i,
                    KeyTime = KeyTime.FromTimeSpan(animSpan)
                });
            }

            // Create storyboard to tie animations together
            //for testing only
            Storyboard sb = new Storyboard { RepeatBehavior = RepeatBehavior.Forever };

            sb.Children.Add(animation);

            Storyboard.SetTarget(animation, translateTransform);
            Storyboard.SetTargetProperty(animation, new PropertyPath(TranslateTransform.XProperty));

            return sb;
        }

        private void CreateAnimations(TranslateTransform spriteTranslateTransform)
        {
            _currentAnimation = CreateAnimationSequence(spriteTranslateTransform);
        }

    }
}
