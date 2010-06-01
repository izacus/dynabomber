using System;
using System.IO;
using DynaBomber_Server.Interop.ServerMsg;

namespace DynaBomber_Server
{
    public static class Util
    {
        public static readonly Point MapOffset = new Point(76, 30);
        public static readonly int BrickSize = 32;

        /// <summary>
        /// Serializes the object into protobuf byte data
        /// </summary>
        public static byte[] SerializeUpdate(IServerUpdate update)
        {
            MemoryStream ms = new MemoryStream();
            update.Serialize(ms);

            return ms.GetBuffer();
        }

        public static Point ToGridCoordinates(Point coordinates)
        {
            int x = (coordinates.X + (BrickSize / 2));
            int y = (coordinates.Y + (BrickSize / 2));

            x -= MapOffset.X;
            y -= MapOffset.Y;

            x = x / BrickSize;
            y = y / BrickSize;

            Point outPt = new Point(x, y);

            return outPt;
        }

        public static Point ToRealCoordinates(Point GridCoordinates)
        {
            int x = GridCoordinates.X;
            int y = GridCoordinates.Y;

            x = x*BrickSize;
            y = y*BrickSize;

            x += MapOffset.X;
            y += MapOffset.Y;

            return new Point(x, y);
        }

    }

    public class Point : IComparable
    {
        public Point (int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; set; }
        public int Y { get; set; }

        public bool Equals(Point other)
        {
            if (other == null) return false;

            return other.X == X && other.Y == Y;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (obj.GetType() != typeof (Point)) return false;

            return Equals((Point) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X*397) ^ Y;
            }
        }

        public int CompareTo(object obj)
        {
            return (Equals(obj) ? 0 : 1);
        }

        public static bool operator == (Point p1, Point p2)
        {
            return p1.CompareTo(p2) == 0;
        }

        public static bool operator !=(Point p1, Point p2)
        {
            return !(p1 == p2);
        }

        public override string ToString()
        {
            return "X: " + X + " Y: " + Y;
        }
    }
}
