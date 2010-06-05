using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Xml.Serialization;
using DynaBomberClient.Communication.ServerMsg;
using DynaBomberClient.MainGame.Brick;

namespace DynaBomberClient
{
    public static class Util
    {
        /// <summary>
        /// Serializes the object into UTF-16 XML 
        /// </summary>
        /// <param name="obj">Object to serialize</param>
        /// <returns>Raw byte string data of XML</returns>
        public static byte[] SerializeObject(object obj)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(obj.GetType());
            StringWriter writer = new StringWriter();

            xmlSerializer.Serialize(writer, obj);
            writer.Flush();

            // Send as little-endian UTF-16 string because the Serializer denotes XML as utf-16
            // which cannot be easly changed
            UnicodeEncoding encoder = new UnicodeEncoding(false, false);
            return encoder.GetBytes(writer.ToString());
        } 

        public static object DeserializeXml(string xmlData, Type type)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(type);

            StringReader reader = new StringReader(xmlData);
            object obj = xmlSerializer.Deserialize(reader);
            
            reader.Dispose();

            return obj;
        }

        public static void InvokeIfNeeded(this DependencyObject ctl, Action action)
        {
            if (ctl.CheckAccess())
            {
                action();
            }
            else
            {
                ctl.Dispatcher.BeginInvoke(action);
            }
        }

        /// <summary>
        /// Creates a keyframe animation sequence
        /// </summary>
        /// <param name="translateTransform">Translate transform for sprite sheet</param>
        /// <param name="startIndex">Index of first animation frame</param>
        /// <param name="numberOfFrames">Number of frames of animation</param>
        /// <param name="ifRepeatForever">Should the animation repeat forever</param>
        /// <param name="tickMs">Milliseconds for each animation tick</param>
        /// <param name="spriteSize">Width of the sprite in sheet</param>
        /// <returns></returns>
        public static Storyboard CreateAnimationSequence(TranslateTransform translateTransform, int startIndex,
                                                          int numberOfFrames, bool ifRepeatForever, int tickMs, int spriteSize)
        {
            DoubleAnimationUsingKeyFrames animation = new DoubleAnimationUsingKeyFrames();
            Storyboard sb = new Storyboard();

            for (int i = 0; i < numberOfFrames; i++)
            {
                TimeSpan animSpan = new TimeSpan(0, 0, 0, 0, i * tickMs);

                // Add keyframe to animation
                animation.KeyFrames.Add(new DiscreteDoubleKeyFrame
                                            {
                                                Value = -spriteSize * (i + startIndex),
                                                KeyTime = KeyTime.FromTimeSpan(animSpan)
                                            });
            }

            if (ifRepeatForever)
            {
                sb.RepeatBehavior = RepeatBehavior.Forever;
            }

            sb.Children.Add(animation);

            Storyboard.SetTarget(animation, translateTransform);
            Storyboard.SetTargetProperty(animation, new PropertyPath(TranslateTransform.XProperty));

            return sb;
        }

        public static Point GetAbsoluteCoordinates(Point gridCoordinates)
        {
            // Find corresponding brick
            int absX = ((int)gridCoordinates.X * Brick.BrickWidth) + Map.Xoffset;
            int absY = ((int)gridCoordinates.Y * Brick.BrickHeight) + Map.Yoffset;

            return new Point(absX, absY);
        }

        public static Point GetRelativeCoordinates(Point absoluteCoordinates)
        {
            int relX = ((int) (absoluteCoordinates.X - Map.Xoffset)/Brick.BrickWidth) ;
            int relY = ((int) (absoluteCoordinates.Y - Map.Yoffset)/Brick.BrickHeight);

            return new Point(relX, relY);
        }
    }
}
