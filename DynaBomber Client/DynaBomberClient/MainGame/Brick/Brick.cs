using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DynaBomberClient.MainGame.Brick
{
    public class Brick : GameObject
    {
        public const int BrickWidth = 32;
        public const int BrickHeight = 32;

        protected Rectangle _spriteRect;
        protected string Filename = "Brick2";

        public Brick(Canvas gameArea, int x, int y)
        {
            position.X = x;
            position.Y = y;
            BrickSprite(gameArea);
        }
        
        public Brick(){}

        public Brick(Canvas gameArea, int x, int y,string newFilename)
        {
            Filename = newFilename;
            position.X = x;
            position.Y = y;
            BrickSprite(gameArea);            
        }

        public Rectangle SpriteRect
        {
            get { return _spriteRect; }
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
                Width = BrickHeight
            };

            //_spriteRect.Stroke = new SolidColorBrush(Colors.Orange);
            //_spriteRect.StrokeThickness = 2;

            ImageBrush spriteSheet = new ImageBrush
            {
                //Stretch = Stretch.None,
                Stretch = Stretch.None,
                AlignmentX = AlignmentX.Left,
                AlignmentY = AlignmentY.Top
            };

            // Prepare transform for sprite animation
            //TranslateTransform spriteSheetPosition = new TranslateTransform();
            //spriteSheet.Transform = spriteSheetPosition;

            // Load sprite sheet image
            spriteSheet.ImageSource = ResourceHelper.GetBitmap("Graphics/Brick/" + Filename + ".png");
            _spriteRect.Fill = spriteSheet;

            parent.Children.Add(_spriteRect);            
            Canvas.SetLeft(_spriteRect, position.X);
            Canvas.SetTop(_spriteRect, position.Y);               
        }

        public Rectangle ContourRectangle()
        {            
            return SpriteRect;
        }
    }
}
