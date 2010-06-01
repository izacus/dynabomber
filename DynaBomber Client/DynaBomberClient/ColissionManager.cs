using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using DynaBomberClient.Brick;
using DynaBomberClient.MainGame.Players;

namespace DynaBomberClient
{
    public static class CollisionManager
    {
        public static SolidColorBrush RedColor = new SolidColorBrush(Colors.Red);

        public static bool RectIntersect(Rectangle rectangle1, Rectangle rectangle2)
        {
            return (((double)rectangle1.GetValue(Canvas.LeftProperty) <= (double)rectangle2.GetValue(Canvas.LeftProperty) + rectangle2.Width)
              && ((double)rectangle1.GetValue(Canvas.LeftProperty) + rectangle1.Width >= (double)rectangle2.GetValue(Canvas.LeftProperty))
              && ((double)rectangle1.GetValue(Canvas.TopProperty) <= (double)rectangle2.GetValue(Canvas.TopProperty) + rectangle2.Height)
              && ((double)rectangle1.GetValue(Canvas.TopProperty) + rectangle1.Height >= (double)rectangle2.GetValue(Canvas.TopProperty)));
        }

        public static void DirectionIntersect(Player player, Rectangle rectangle1, GameObject other)
        {
            if (other is DestroyableBrick &&
                ((DestroyableBrick)other).Destroyed)
            {
                return;
            }

            Rectangle rectangle2 = other.GetRectangle();

            double x1 = (double)rectangle1.GetValue(Canvas.LeftProperty);
            double y1 = (double)rectangle1.GetValue(Canvas.TopProperty);
            double w1 = rectangle1.Width;
            double h1 = rectangle1.Height;

            double x2 = (double)rectangle2.GetValue(Canvas.LeftProperty);
            double y2 = (double)rectangle2.GetValue(Canvas.TopProperty);
            double w2 = rectangle2.Width;
            double h2 = rectangle2.Height;

            Point tmp = ResourceHelper.ToGridCoordinates(new Point(x1, y1));
            Point playersGridCoord = new Point(Math.Floor(tmp.X), Math.Floor(tmp.Y));
            tmp = ResourceHelper.ToGridCoordinates(new Point(x2, y2));
            Point otherGridCoords = new Point(Math.Floor(tmp.X), Math.Floor(tmp.Y));

            bool collide = RectIntersect(rectangle1, rectangle2);


            if (playersGridCoord.X != otherGridCoords.X)
            {
                if (collide && playersGridCoord.X > otherGridCoords.X)
                {
                    player.X += x2 + w2 - x1 + 1;
                }
                else if (collide && playersGridCoord.X < otherGridCoords.X)
                {
                    player.X -= x1 + w1 - x2 + 1;
                }
            }

            else if (playersGridCoord.Y != otherGridCoords.Y)
            {
                if (collide && playersGridCoord.Y > otherGridCoords.Y)
                {
                    player.Y += y2 + h2 - y1 + 1;
                }
                else if (collide && playersGridCoord.Y < otherGridCoords.Y)
                {
                    player.Y -= y1 + h1 - y2 + 1;
                }
            }
        }
    }
}
