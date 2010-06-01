using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Serialization;
using DynaBomberClient.Brick;
using ProtoBuf;

namespace DynaBomberClient.MainGame.Server
{
    public enum TileType
    {
        Wall = 0,
        Brick = 1,
        Grass = 2
    }

    [ProtoContract]
    public class Map
    {
        public const int Xoffset = 76;
        public const int Yoffset = 30;

        [ProtoIgnore]
        private int _sizeX;
        [ProtoIgnore]
        private int _sizeY;

        public Map()
        {
            this.TileData = new List<int>();
        }


        public List<Brick.Brick> CreateBricks(Canvas gameCanvas)
        {
            List<Brick.Brick> bricks = new List<Brick.Brick>();

            for (int x = 0; x < _sizeX; x++)
            {
                for (int y = 0; y < _sizeY; y++)
                {
                    // Delegate object creation to UI thread
                    int bX = Xoffset + (x * Brick.Brick.BrickWidth);
                    int bY = Yoffset + (y * Brick.Brick.BrickHeight);

                    switch(GetTile(x, y))
                    {
                        case TileType.Grass:
                            break;
                        case TileType.Brick:
                            Deployment.Current.Dispatcher.BeginInvoke(() => AddBrick(gameCanvas, true, bX, bY, bricks));
                            break;
                        case TileType.Wall:
                            Deployment.Current.Dispatcher.BeginInvoke(() => AddBrick(gameCanvas, false, bX, bY, bricks));
                            break;
                    }
                }
            }

            return bricks;
        }

        private void AddBrick(Canvas canvas, Boolean destroyable, int x, int y, List<Brick.Brick> brickList)
        {
            Brick.Brick brick;

            if (destroyable)
            {
                brick = new DestroyableBrick(this, canvas, x, y);
            }
            else
            {
                brick = new Brick.Brick(canvas, x, y);
            }

            lock(brickList)
            {
                brickList.Add(brick);
            }

            Debug.WriteLine("Created brick on " + x + " " + y);
        }

        public void ClearBrick(int x, int y)
        {
            SetTile(x, y, TileType.Grass);
        }

        public Boolean IsGrass(int x, int y)
        {
            if (x < 0 || x > SizeX - 1)
                return false;

            if (y < 0 || y > SizeY - 1)
                return false;

            return GetTile(x, y) == TileType.Grass;
        }

        public TileType GetTile(int x, int y)
        {
            return (TileType)TileData[y*SizeX + x];
        }

        public void SetTile(int x, int y, TileType type)
        {
            TileData[y*SizeX + x] = (int)type;
        }

        #region Interface
        /// <summary>
        /// These methods are required because of the way how XML serialization works
        /// If first invokes parameterless constructor and then calls property methods to populate them
        /// 
        /// These methods MUST be the same as those in the server side interop class
        /// </summary>

        [ProtoMember(1)]
        public int SizeX
        {
            get { return _sizeX; }
            set { _sizeX = value; }
        }

        [ProtoMember(2)]
        public int SizeY
        {
            get { return _sizeY; }
            set { _sizeY = value; }
        }

        [ProtoMember(3)]
        public List<int> TileData { get; set; }

        #endregion
    }
}
