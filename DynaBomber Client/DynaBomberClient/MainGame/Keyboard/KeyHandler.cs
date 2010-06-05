using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace  DynaBomberClient.MainGame.Keyboard
{
    /*
        Code originally from http://www.bluerosegames.com/silverlight-games-101/ 
    */

    public class KeyHandler
    {
        protected static KeyHandler instance;
        Dictionary<Key, bool> isPressed = new Dictionary<Key, bool>();
        FrameworkElement _targetElement = null;

        public static KeyHandler Instance
        {
            get { return instance ?? (instance = new KeyHandler()); }
        }

        public void ClearKeyPresses()
        {
            isPressed.Clear();
        }

        public void StartupKeyHandler(FrameworkElement target)
        {
            ClearKeyPresses();
            _targetElement = target;
            target.KeyDown += TargetKeyDown;
            target.KeyUp += TargetKeyUp;
            target.LostFocus += TargetLostFocus;
        }

        public void Shutdown()
        {
            ClearKeyPresses();
            _targetElement.KeyDown -= TargetKeyDown;
            _targetElement.KeyUp -= TargetKeyUp;
            _targetElement.LostFocus -= TargetLostFocus;
            _targetElement = null;
        }

        void TargetKeyDown(object sender, KeyEventArgs e)
        {
            if (!isPressed.ContainsKey(e.Key))
            {
                isPressed.Add(e.Key, true);
            }
        }

        void TargetKeyUp(object sender, KeyEventArgs e)
        {
            if (isPressed.ContainsKey(e.Key))
            {
                isPressed.Remove(e.Key);
            }
        }

        void TargetLostFocus(object sender, RoutedEventArgs e)
        {
            ClearKeyPresses();
        }

        public bool IsKeyPressed(Key k)
        {
            return isPressed.ContainsKey(k);
        }
    }
}