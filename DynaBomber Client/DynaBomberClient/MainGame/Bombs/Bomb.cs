using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using DynaBomberClient.MainGame.Communication;
using DynaBomberClient.MainGame.Communication.ServerMsg;

namespace DynaBomberClient.MainGame.Bombs
{
    public class Bomb : GameObject
    {
        private const int BombHeight = 32;
        private const int BombWidth = 32;

        // Explosion
        private const int ExplosionTicks = 150;
        private const int AnimationTickMs = 450;
        private const string Filename = "BombAnimation";
        private Map _mapOfBricks;

        //Animation
        private Canvas _gameCanvas;

        private Rectangle _bombRect;
        private Storyboard[] _explosionAnimation;
        private Rectangle[] _explosionRectangles;
        private Point[] _positionOfExplosion;
        private Storyboard _tickingAnimation;


        public Bomb(Canvas gameCanvas, int x, int y)
        {
            _gameCanvas = gameCanvas;

            Range = 2;
            inUse = true;

            position = new Point(x, y);
            BombSprite(_gameCanvas);

            Canvas.SetLeft(_bombRect, x);
            Canvas.SetTop(_bombRect, y);
        }

        private void CreateExplosionSequence(int range)
        {
            double x = position.X;
            double y = position.Y;

            _explosionRectangles = new Rectangle[4 * range + 1];
            _explosionAnimation = new Storyboard[4 * range + 1];

            _positionOfExplosion = CalculatePositionsOfExplosion(x, y, range);

            for (int i = 0; i < (range * 4) + 1; i++)
            {
                _explosionRectangles[i] = new Rectangle
                                              {
                                                  Width = BombWidth,
                                                  Height = BombHeight,
                                                  Visibility = Visibility.Collapsed
                                              };

                Canvas.SetLeft(_explosionRectangles[i], _positionOfExplosion[i].X);
                Canvas.SetTop(_explosionRectangles[i], _positionOfExplosion[i].Y);

                _gameCanvas.Children.Add(_explosionRectangles[i]);

                // Select correct explosion animation image
                string imageFilename = GetExplosionAnimationFilename(i, range);

                CreateExplosionBlock(_explosionRectangles[i], imageFilename, out _explosionAnimation[i]);

            }
        }

        private string GetExplosionAnimationFilename(int index, int range)
        {
            // Calulation is "1-based" so first tile is "Index 1" not "Index 0"
            // that's why we have to add +1 when calculating

            // "Tip" of the explosion
            Boolean isEndPoint = (index % range == (range - 1));

            int direction = index / range;

            switch (direction)
            {
                // LEFT
                case 0:
                    return isEndPoint ? "ExplosionLeft" : "ExplosionHorizontal";
                // RIGHT
                case 1:
                    return isEndPoint ? "ExplosionRight" : "ExplosionHorizontal";
                // UP
                case 2:
                    return isEndPoint ? "ExplosionUp" : "ExplosionVertical";
                // DOWN
                case 3:
                    return isEndPoint ? "ExplosionDown" : "ExplosionVertical";

                case 4:
                    return "ExplosionCentre";

                default:
                    throw new NotImplementedException("Error while computing explosion sprites.");
            }
        }
        //change it 
        private Point[] CalculatePositionsOfExplosion(double x, double y, int range)
        {
            int size = BombHeight;
            //add constraints
            var locations = new Point[4 * range + 1];

            int index = 0;

            // Left
            for (int i = 0; i < range; i++)
            {
                locations[index++] = new Point(x - size * (i + 1), y);
            }

            // Right
            for (int i = 0; i < range; i++)
            {
                locations[index++] = new Point(x + size * (i + 1), y);
            }

            // Up
            for (int i = 0; i < range; i++)
            {
                locations[index++] = new Point(x, y - size * (i + 1));
            }

            // Down
            for (int i = 0; i < range; i++)
            {
                locations[index++] = new Point(x, y + size * (i + 1));
            }

            // Center
            locations[index] = new Point(x, y);

            return locations;
        }

        public override Rectangle GetRectangle()
        {
            return _bombRect;
        }

        private void CreateExplosionBlock(Rectangle rect, string imageFilename, out Storyboard animation)
        {
            var spriteSheet = new ImageBrush
                                  {
                                      Stretch = Stretch.None,
                                      AlignmentX = AlignmentX.Left,
                                      AlignmentY = AlignmentY.Top
                                  };

            // Prepare transform for sprite animation
            var spriteSheetPosition = new TranslateTransform();
            spriteSheet.Transform = spriteSheetPosition;

            // Load sprite sheet image
            spriteSheet.ImageSource = ResourceHelper.GetBitmap("Graphics/Bomb/Explosion/" + imageFilename + ".png");
            rect.Fill = spriteSheet;

            animation = CreateAnimationSequence(spriteSheetPosition, 7, ExplosionTicks);
        }

        public void Explode()
        {
            CreateExplosionSequence(Range);

            Point tmp = ResourceHelper.ToGridCoordinates(position);
            int xPos = (int)tmp.X;
            int yPos = (int)tmp.Y;

            //Left
            for (int i = 1; i <= Range; i++)
            {
                if (_mapOfBricks.IsGrass(xPos - i, yPos) == false)
                    break;
                
                _explosionRectangles[i - 1].Visibility = Visibility.Visible;
            }

            //Right
            for (int i = 1; i <= Range; i++)
            {
                if (_mapOfBricks.IsGrass(xPos + i, yPos) == false)
                    break;
                
                _explosionRectangles[Range + (i - 1)].Visibility = Visibility.Visible;
            }

            //Up
            for (int i = 1; i <= Range; i++)
            {
                if (_mapOfBricks.IsGrass(xPos, yPos - i) == false)
                    break;

                _explosionRectangles[Range*2 + (i - 1)].Visibility = Visibility.Visible;
            }

            //Down
            for (int i = 1; i <= Range; i++)
            {
                if (_mapOfBricks.IsGrass(xPos, yPos + i) == false)
                    break;
                
                
                _explosionRectangles[Range * 3 + ( i - 1)].Visibility = Visibility.Visible;
            }

            _explosionRectangles[4 * Range].Visibility = Visibility.Visible;

            _tickingAnimation.Stop();

            _gameCanvas.Children.Remove(_bombRect);

            foreach (Storyboard foo in _explosionAnimation)
            {
                foo.Begin();
            }

            _explosionAnimation[0].Completed += Terminate;
        }

        public void BombSprite(Canvas parent)
        {
            _bombRect = new Rectangle
                            {
                                Width = BombWidth,
                                Height = BombHeight
                            };

            var spriteSheet = new ImageBrush
                                  {
                                      Stretch = Stretch.None,
                                      AlignmentX = AlignmentX.Left,
                                      AlignmentY = AlignmentY.Top
                                  };


            // Prepare transform for sprite animation
            var spriteSheetPosition = new TranslateTransform();
            spriteSheet.Transform = spriteSheetPosition;

            // Load sprite sheet image
            spriteSheet.ImageSource = ResourceHelper.GetBitmap("Graphics/Bomb/" + Filename + ".png");
            _bombRect.Fill = spriteSheet;

            // Create sprite animations
            _tickingAnimation = CreateAnimationSequence(spriteSheetPosition);

            _tickingAnimation.Seek(TimeSpan.Zero);
            _tickingAnimation.Begin();

            parent.Children.Add(_bombRect);
            Canvas.SetLeft(_bombRect, position.X);
            Canvas.SetTop(_bombRect, position.Y);
        }

        private Storyboard CreateAnimationSequence(TranslateTransform translateTransform)
        {
            var animation = new DoubleAnimationUsingKeyFrames();

            for (int i = 1; i <= 3; i++)
            {
                var animSpan = new TimeSpan(0, 0, 0, 0, i * AnimationTickMs);

                // Add keyframe to animation
                animation.KeyFrames.Add(new DiscreteDoubleKeyFrame
                                            {
                                                Value = -BombHeight * i,
                                                KeyTime = KeyTime.FromTimeSpan(animSpan)
                                            });
            }

            // Create storyboard to tie animations together
            var sb = new Storyboard { RepeatBehavior = RepeatBehavior.Forever };

            sb.Children.Add(animation);

            Storyboard.SetTarget(animation, translateTransform);
            Storyboard.SetTargetProperty(animation, new PropertyPath(TranslateTransform.XProperty));

            return sb;
        }

        private Storyboard CreateAnimationSequence(TranslateTransform translateTransform,
                                                   int numberOfFrames, int tickMs)
        {
            var animation = new DoubleAnimationUsingKeyFrames();
            var sb = new Storyboard();

            for (int i = 0; i < numberOfFrames; i++)
            {
                var animSpan = new TimeSpan(0, 0, 0, 0, i * tickMs);

                // Add keyframe to animation
                animation.KeyFrames.Add(new DiscreteDoubleKeyFrame
                                            {
                                                Value = -BombHeight * i,
                                                KeyTime = KeyTime.FromTimeSpan(animSpan)
                                            });
            }

            sb.Children.Add(animation);

            Storyboard.SetTarget(animation, translateTransform);
            Storyboard.SetTargetProperty(animation, new PropertyPath(TranslateTransform.XProperty));

            return sb;
        }

        private void Terminate(object sender, EventArgs e)
        {
            ShutDown();
        }

        public override void ShutDown()
        {
            for (int i = 0; i < Range * 4 + 1; i++)
            {
                _gameCanvas.Children.Remove(_explosionRectangles[i]);
            }

            inUse = false;
        }

        public int Range { get; set; }

        public Map MapToCheck
        {
            set { _mapOfBricks = value; }
        }
    }
}