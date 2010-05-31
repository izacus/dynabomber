using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using DynaBomber_Server.Interop;
using ProtoBuf;

namespace DynaBomber_Server.GameClasses
{
    public enum MovementDirection
    {
        Left,
        Right,
        Up,
        Down
    }

    [ProtoContract]
    public class Player : IUpdate
    {
        private const int MaxBombRange = 10;

        private int _bombRange;

        public Player()
        {
            Init();
        }

        public Player(PlayerColors color)
        {
            Init();

            Color = color;

            // Setup player starting position depending on the color
            switch (color)
            {
                case PlayerColors.Cyan:
                    X = 67;
                    Y = 32;
                    Direction = MovementDirection.Down;
                    break;

                case PlayerColors.Red:
                    X = 516;
                    Y = 416;
                    Direction = MovementDirection.Up;
                    break;

                case PlayerColors.Green:
                    X = 67;
                    Y = 416;
                    Direction = MovementDirection.Up;
                    break;

                case PlayerColors.Blue:
                    X = 516;
                    Y = 32;
                    Direction = MovementDirection.Down;
                    break;

                default:
                    X = 200;
                    Y = 200;
                    break;
            }
        }

        private void Init()
        {
            _bombRange = 2;
            BombNumber = 1;
            ManualTrigger = false;
            Dead = false;
        }


        #region Interface

        /// <summary>
        /// Players color
        /// </summary>
        [ProtoMember(1, Name = "Color")]
        public PlayerColors Color { get; set; }

        /// <summary>
        /// Player position in absolute coordinates
        /// </summary>
        [ProtoMember(2, Name = "X")]
        public int X { get; set; }
        
        /// <summary>
        /// Player position in absolute coordinates
        /// </summary>
        [ProtoMember(3, Name = "Y")]
        public int Y { get; set; }

        /// <summary>
        /// Current player facing
        /// </summary>
        [ProtoMember(4, Name = "Direction")]
        public MovementDirection Direction { get; set; }

        /// <summary>
        /// Is player's moving animation currently playing?
        /// </summary>
        [ProtoMember(5, Name = "Moving")]
        public bool Moving { get; set; }

        /// <summary>
        /// Range of the players bombs
        /// </summary>
        [ProtoIgnore]
        public int BombRange
        {
            get { return _bombRange; }
            set { this._bombRange = value < MaxBombRange ? value : MaxBombRange; } 
        }

        /// <summary>
        /// Number of bombs player can set at once
        /// </summary>
        [ProtoIgnore]
        public int BombNumber { get; set; }

        /// <summary>
        /// Whether player bombs are triggered by timer or by command
        /// </summary>
        [ProtoIgnore]
        public bool ManualTrigger { get; set; }

        /// <summary>
        /// Is player dead?
        /// </summary>
        
        [ProtoIgnore]
        public bool Dead { get; set; }

        [ProtoIgnore]
        public Point GridPosition
        {
            get
            {
                return Util.ToGridCoordinates(new Point(X, Y));
            }
        }

        #endregion

        public void Serialize(MemoryStream ms)
        {
            ms.WriteByte((byte)MessageType.Player);
            Serializer.SerializeWithLengthPrefix(ms, this, PrefixStyle.Base128);
        }
    }
}
