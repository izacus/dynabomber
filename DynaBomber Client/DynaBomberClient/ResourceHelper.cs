using System;
using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using System.Windows.Resources;

namespace DynaBomberClient
{
    public class ResourceHelper
    {
        public static readonly Point MapOffset = new Point(76, 30);
        public static readonly int BrickSize = 32;

        public static string ExecutingAssemblyName
        {
            get
            {
                string name = System.Reflection.Assembly.GetExecutingAssembly().FullName;
                return name.Substring(0, name.IndexOf(','));
            }
        }

        public static Stream GetStream(string relativeUri, string assemblyName)
        {
            StreamResourceInfo res = Application.GetResourceStream(new Uri(assemblyName + ";component/" + relativeUri, UriKind.Relative)) ??
                                     Application.GetResourceStream(new Uri(relativeUri, UriKind.Relative));

            return res != null ? res.Stream : null;
        }

        public static Stream GetStream(string relativeUri)
        {
            return GetStream(relativeUri, ExecutingAssemblyName);
        }

        public static BitmapImage GetBitmap(string relativeUri, string assemblyName)
        {
            Stream s = GetStream(relativeUri, assemblyName);
            if (s == null) return null;
            using (s)
            {
                BitmapImage bmp = new BitmapImage();
                bmp.SetSource(s);
                return bmp;
            }
        }

        public static BitmapImage GetBitmap(string relativeUri)
        {
            return GetBitmap(relativeUri, ExecutingAssemblyName);
        }

        public static string GetString(string relativeUri, string assemblyName)
        {
            Stream s = GetStream(relativeUri, assemblyName);
            if (s == null) return null;
            using (StreamReader reader = new StreamReader(s))
            {
                return reader.ReadToEnd();
            }
        }

        public static string GetString(string relativeUri)
        {
            return GetString(relativeUri, ExecutingAssemblyName);
        }

        public static FontSource GetFontSource(string relativeUri, string assemblyName)
        {
            Stream s = GetStream(relativeUri, assemblyName);
            if (s == null) return null;
            using (s)
            {
                return new FontSource(s);
            }
        }

        public static FontSource GetFontSource(string relativeUri)
        {
            return GetFontSource(relativeUri, ExecutingAssemblyName);
        }

        public static object GetXamlObject(string relativeUri, string assemblyName)
        {
            string str = GetString(relativeUri, assemblyName);
            if (str == null) return null;
            object obj = System.Windows.Markup.XamlReader.Load(str);
            return obj;
        }

        public static object GetXamlObject(string relativeUri)
        {
            return GetXamlObject(relativeUri, ExecutingAssemblyName);
        }

        public static Point ToGridCoordinates(Point coordinates)
        {
            double x = (coordinates.X + (BrickSize / 2.0));
            double y = (coordinates.Y + (BrickSize / 2.0));

            x -= MapOffset.X;
            y -= MapOffset.Y;

            x = x / BrickSize;
            y = y / BrickSize;

            return new Point(x, y);
        }

        public static Point ToRealCoordinates(Point gridCoordinates)
        {
            double x = gridCoordinates.X;
            double y = gridCoordinates.Y;

            x = x * BrickSize;
            y = y * BrickSize;

            x += MapOffset.X;
            y += MapOffset.Y;

            return new Point(x, y);
        }

        public static Point ToGridCoordinates(Point coordinates,double brickWidth,double brickHeight)
        {
            double x = (coordinates.X + (BrickSize / 2.0));
            double y = (coordinates.Y + (BrickSize / 2.0));

            x -= MapOffset.X;
            y -= MapOffset.Y;

            x = x/brickWidth;
            y = y/brickHeight;

            return new Point(x, y);
        }

        public static Point ToRealCoordinates(Point gridCoordinates,double brickWidth,double brickHeight)
        {
            double x = gridCoordinates.X;
            double y = gridCoordinates.Y;

            x = x*brickWidth;
            y = y*brickHeight;

            x += MapOffset.X;
            y += MapOffset.Y;

            return new Point(x, y);
        }

    }
}