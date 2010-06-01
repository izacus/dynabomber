using System;

namespace DynaBomber_Server.GameClasses
{
    /// <summary>
    /// Represents a set bomb
    /// </summary>
    public class Bomb
    {
        private readonly PlayerColors _ownerColor;
        // Timer is in milliseconds
        private readonly int _timer;
        private readonly DateTime _setupTime;

        private bool _triggered = false;

        /// <summary>
        /// Sets up a new bomb
        /// </summary>
        /// <param name="gridPosition">Position of the bomb in grid coordinates</param>
        /// <param name="timer">Explosion timer, 0 if manually triggered</param>
        /// <param name="range">Range of the bomb explosion</param>
        /// <param name="ownerColor">Color of the owner</param>
        public Bomb(Point gridPosition, int timer, int range, PlayerColors ownerColor)
        {
            Position = gridPosition;
            _timer = timer;
            _setupTime = DateTime.Now;
            _ownerColor = ownerColor;
            Range = range;
        }

        /// <summary>
        /// Is the bomb ready to explode
        /// </summary>
        /// <returns>true if the bomb was triggered</returns>
        public Boolean IsTimeUp()
        {
            if (_timer > 0)
            {
                TimeSpan span = DateTime.Now - _setupTime;
                return span.TotalMilliseconds > _timer || _triggered;
            }

            return _triggered;
        }

        /// <summary>
        /// Triggers the bomb explosion
        /// </summary>
        public void Trigger()
        {
            _triggered = true;
        }

        /// <summary>
        /// Current bomb position
        /// </summary>
        public Point Position { get; private set; }

        /// <summary>
        /// Range of bombs explosion
        /// </summary>
        public int Range { get; private set; }

        /// <summary>
        /// Color of the bombs owner
        /// </summary>
        public PlayerColors OwnerColor 
        { 
            get
            {
                return _ownerColor;
            }
        }
    }
}
