using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using DynaBomberClient.Keyboard;
using DynaBomberClient.MainGame;
using DynaBomberClient.MainGame.Server;

namespace DynaBomberClient.Player
{
    public enum PlayerColor
    {
        Cyan,
        Green,
        Red,
        Blue,
        None
    }

    public enum MovementDirection
    {
        Left,
        Right,
        Up,
        Down
    }

    /// <summary>
    /// Represents a single player, either local or remote
    /// </summary>
    public class Player : GameObject
    {
        private const int ScrambledControlsSec = 10;
        private readonly DispatcherTimer _scrambleTimer;
        protected readonly PlayerSprite _sprite;
        private Boolean _bombKeyDown;

        // List of powerups kept by player
        //private Powerup _collectedPowerup;

        // Unique ID used to distinguish remote players
        protected int _id;

        // Active?
        protected Boolean _isDead;

        // Player sprite to draw
        protected bool _isMoving;
        protected MainGameState _mainState;
        protected MovementDirection _movementDirection;

        private Boolean _scrambledControls;
        private double _speed = 3;

        public Player(MainGameState mainState, PlayerColor color, int x, int y)
        {
            _isDead = false;

            Color = color;
            _mainState = mainState;

            // Create the player's sprite
            _sprite = new PlayerSprite(_mainState.GameCanvas, color);
            position = new Point(x, y);

            _sprite.X = x;
            _sprite.Y = y;

            _movementDirection = MovementDirection.Down;
            _isMoving = false;

            _scrambleTimer = new DispatcherTimer();
            _scrambleTimer.Interval = new TimeSpan(0, 0, 0, ScrambledControlsSec);
            _scrambleTimer.Tick += UnscrambleControls;
        }

        public override void Display()
        {
            if (_mainState.State == RunStates.WaitingForGameStart)
            {
                if (KeyHandler.Instance.IsKeyPressed(Key.Space))
                {
                    Debug.WriteLine("Sending ready command...");
                    _mainState.Server.SendStartRequest();
                }
            }

            if (_mainState.State != RunStates.GameInProgress)
                return;

            // Dead players don't move
            if (_isDead)
                return;

            if (KeyHandler.Instance.IsKeyPressed(Key.Up) || KeyHandler.Instance.IsKeyPressed(Key.W))
            {
                Move(_scrambledControls ? MovementDirection.Down : MovementDirection.Up);
            }
            else if (KeyHandler.Instance.IsKeyPressed(Key.Down) || KeyHandler.Instance.IsKeyPressed(Key.S))
            {
                Move(_scrambledControls ? MovementDirection.Up : MovementDirection.Down);
            }

            else if (KeyHandler.Instance.IsKeyPressed(Key.Left) || KeyHandler.Instance.IsKeyPressed(Key.A))
            {
                Move(_scrambledControls ? MovementDirection.Right : MovementDirection.Left);
            }
            else if (KeyHandler.Instance.IsKeyPressed(Key.Right) || KeyHandler.Instance.IsKeyPressed(Key.D))
            {
                Move(_scrambledControls ? MovementDirection.Left : MovementDirection.Right);
            }
            else
            {
                _sprite.Stop();
            }

            // Bomb set key has to work while moving as well
            if (KeyHandler.Instance.IsKeyPressed(Key.Space))
            {
                if (!_bombKeyDown)
                {
                    SetBomb();
                    _bombKeyDown = true;
                }
            }
            else
            {
                _bombKeyDown = false;
            }

            // Remote bomb trigger
            if (KeyHandler.Instance.IsKeyPressed(Key.Alt))
            {
                _mainState.Server.SendTriggerCommand();
            }
        }

        private void Move(MovementDirection where)
        {
            switch (where)
            {
                case MovementDirection.Up:
                    UpdateDirection(MovementDirection.Up);
                    if (40 < Y) 
                        Y -= _speed;
                    break;

                case MovementDirection.Down:
                    UpdateDirection(MovementDirection.Down);
                    if (Y < 425)
                        Y += _speed;
                    break;

                case MovementDirection.Left:
                    UpdateDirection(MovementDirection.Left);
                    if (67 < X)
                        X -= _speed;
                    break;

                case MovementDirection.Right:
                    UpdateDirection(MovementDirection.Right);
                    if (X < 515) 
                        X += _speed;
                    break;
            }
        }


        public Rectangle ContourRectangle()
        {
            return _sprite.SpriteRect;
        }

        public void SetBomb()
        {
            // Send bomb set notification to server
            _mainState.Server.SendBombSetNotify(Position);
        }

        public void ScrambleControls()
        {
            _scrambledControls = true;

            Deployment.Current.Dispatcher.BeginInvoke(_scrambleTimer.Start);
        }

        private void UnscrambleControls(object sender, EventArgs eventArgs)
        {
            _scrambledControls = false;
            _scrambleTimer.Stop();
        }

        protected void UpdateDirection(MovementDirection direction)
        {
            // Dead players don't move
            if (_isDead)
                return;

            _movementDirection = direction;
            _isMoving = true;

            // Update sprite animation on movement direction change
            switch (direction)
            {
                case MovementDirection.Left:
                    _sprite.GoLeft();
                    break;
                case MovementDirection.Right:
                    _sprite.GoRight();
                    break;
                case MovementDirection.Up:
                    _sprite.GoUp();
                    break;
                case MovementDirection.Down:
                    _sprite.GoDown();
                    break;
                default:
                    break;
            }
        }

        public override Rectangle GetRectangle()
        {
            return _sprite.PlayerRect;
        }

        public override void Collide(GameObject other)
        {
            CollisionManager.DirectionIntersect(this, GetRectangle(), other);
        }

        public override void ShutDown()
        {
            _sprite.DeathAnimation.RepeatBehavior = new RepeatBehavior(1);
            _sprite.DeathAnimation.Completed += PlayerDead;
            _sprite.DeathAnimation.Begin();
            _isDead = true;
        }

        private void PlayerDead(object sender, EventArgs e)
        {
            _sprite.Terminate();
        }

        #region Getters and setters

        public double X
        {
            get { return position.X; }
            set
            {
                position.X = value;
                _sprite.X = value;
            }
        }

        public double Y
        {
            get { return position.Y; }
            set
            {
                position.Y = value;
                _sprite.Y = value;
            }
        }

        public MovementDirection Direction
        {
            get { return _movementDirection; }
            set { UpdateDirection(value); }
        }

        public bool Moving
        {
            get { return _isMoving; }
            set
            {
                _isMoving = value;

                if (!_isMoving)
                    _sprite.Stop();
            }
        }

        public double Speed
        {
            get { return _speed; }
            set { _speed = value; }
        }

        public PlayerColor Color { get; private set; }

        public Boolean Dead
        {
            get { return _isDead; }
        }

        #endregion
    }
}