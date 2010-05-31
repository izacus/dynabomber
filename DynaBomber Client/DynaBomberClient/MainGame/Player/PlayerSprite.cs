using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace DynaBomberClient.Player
{
    public class PlayerSprite
    {
        private Canvas _parent;

        private const int SpriteSize = 48;
        private const int spriteHeight = 42;
        private const int spriteWidth = 48;
        private const int AnimationTickMs = 130;

        private readonly Rectangle _spriteRect;
        private Rectangle playerRect;
        private Storyboard _currentAnimation;

        private Storyboard _moveDown;
        private Storyboard _moveRight;
        private Storyboard _moveLeft;
        private Storyboard _moveUp;
        private Storyboard _deathAnimation;


        //for testing
        private Ellipse ellipses;


        /// <summary>
        /// Setup the sprite and add it to the main game canvas
        /// </summary>
        /// <param name="parent">Canvas on which the sprite will be drawn</param>
        /// <param name="color">Color of the player</param>
        public PlayerSprite(Canvas parent, PlayerColor color)
        {
            _parent = parent;

            playerRect = new Rectangle
                             {
                Width = 30,
                Height = 22,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                //Stroke = new SolidColorBrush(Colors.Red)
            };            
            
            _spriteRect = new Rectangle
            {
                Height = SpriteHeight,
                Width = SpriteWidth
            };

            // Sprite sheet holding all animation frames
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
            string filename = "";

            switch (color)
            {
                case PlayerColor.Cyan:
                    filename = "cyan";
                    break;
                case PlayerColor.Blue:
                    filename = "blue";
                    break;
                case PlayerColor.Green:
                    filename = "green";
                    break;
                case PlayerColor.Red:
                    filename = "red";
                    break;
            }

            spriteSheet.ImageSource = ResourceHelper.GetBitmap("Graphics/Player/" + filename + ".png");
            SpriteRect.Fill = spriteSheet;
            
            // Create sprite animations
            CreateAnimations(spriteSheetPosition);

            // Set starting animation position
            _currentAnimation = _moveDown;
            _currentAnimation.Seek(TimeSpan.Zero);

            // Add sprite to canvas
            parent.Children.Add(SpriteRect);

            ellipses = new Ellipse
            {
                Width = 5,
                Height = 5,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };

            Ellipses.Fill = new SolidColorBrush(Colors.Black);            
            parent.Children.Add(PlayerRect);

            Canvas.SetZIndex(_spriteRect,2);
            Canvas.SetZIndex(ellipses, 2);
        }

        public void Terminate()
        {
            _parent.Children.Remove(_spriteRect);
        }

        #region Getters and setters
        /// <summary>
        /// X position of the character
        /// </summary>
        public double X
        {
            get { return Canvas.GetLeft(SpriteRect); }
            set
            {
                Canvas.SetLeft(SpriteRect, value);
                Canvas.SetLeft(PlayerRect, value + 10);
            }
        }

        /// <summary>
        /// Y position of the character
        /// WARNING: the position denotes the position of the "feet" section!
        /// </summary>
        public double Y
        {
            get { return Canvas.GetTop(SpriteRect) + (SpriteSize / 2); }
            set
            {
                Canvas.SetTop(SpriteRect, value - (SpriteSize / 2));
                Canvas.SetTop(PlayerRect, value - (SpriteSize / 2) + 14 + 3);
            }
        }

        public Rectangle SpriteRect
        {
            get { return _spriteRect; }
        }

        public Storyboard DeathAnimation
        {
            get
            {
                _currentAnimation.Stop();
                return _deathAnimation;
            }
        }

        public int SpriteHeight
        {
            get { return spriteHeight; }
        }

        public int SpriteWidth
        {
            get { return spriteWidth; }
        }

        public Rectangle PlayerRect
        {
            get { return playerRect; }
        }

        public Ellipse Ellipses
        {
            get { return ellipses; }
        }

        #endregion

        /// <summary>
        /// Creates sprite movement animations
        /// </summary>
        private void CreateAnimations(TranslateTransform spriteTranslateTransform)
        {
            _moveDown = Util.CreateAnimationSequence(spriteTranslateTransform, 0, 4, true, AnimationTickMs, SpriteSize);
            _moveRight = Util.CreateAnimationSequence(spriteTranslateTransform, 3, 4, true, AnimationTickMs, SpriteSize);
            _moveLeft = Util.CreateAnimationSequence(spriteTranslateTransform, 6, 4, true, AnimationTickMs, SpriteSize);
            _moveUp = Util.CreateAnimationSequence(spriteTranslateTransform, 9, 4, true, AnimationTickMs, SpriteSize);
            
            _deathAnimation = Util.CreateAnimationSequence(spriteTranslateTransform, 12, 9, true, 250, SpriteSize);
        }

        #region Animation control methods

        public void GoDown()
        {
            if (_currentAnimation == _moveDown)
                return;

            _currentAnimation.Stop();
            _currentAnimation = _moveDown;
            _currentAnimation.Begin();
        }

        public void GoUp()
        {
            if (_currentAnimation == _moveUp)
                return;

            _currentAnimation.Stop();
            _currentAnimation = _moveUp;
            _currentAnimation.Seek(TimeSpan.Zero);
            _currentAnimation.Begin();
        }

        public void GoLeft()
        {
            if (_currentAnimation == _moveLeft)
                return;

            _currentAnimation.Stop();
            _currentAnimation = _moveLeft;
            _currentAnimation.Seek(TimeSpan.Zero);
            _currentAnimation.Begin();
        }

        public void GoRight()
        {
            if (_currentAnimation == _moveRight)
                return;

            _currentAnimation.Stop();
            _currentAnimation = _moveRight;
            _currentAnimation.Seek(TimeSpan.Zero);
            _currentAnimation.Begin();
        }

        public void Stop()
        {
            _currentAnimation.Seek(TimeSpan.Zero);
            _currentAnimation.Pause();
        }

        #endregion

    }
}