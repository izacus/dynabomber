using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DynaBomber_Server.GameClasses;
using ProtoBuf;

namespace DynaBomber_Server.Interop.ServerMsg
{
    public enum TileType
    {
        Wall = 0,
        Brick = 1,
        Grass = 2
    }

    [ProtoContract]
    public class Map : IServerUpdate
    {
        [ProtoIgnore]
        private int _sizeX;
        [ProtoIgnore]
        private int _sizeY;
        [ProtoIgnore]
        private TileType[,] _tiles;
        [ProtoIgnore]
        private Powerup[,] _powerups;

        /// <summary>
        /// Private parameterless constructor required for serialization
        /// </summary>
        private Map()
        {}

        public Map(int sizeX, int sizeY)
        {
            // Create tile array
            _tiles = new TileType[sizeX,sizeY];

            _sizeX = sizeX;
            _sizeY = sizeY;

            // Create powerup array
            _powerups = new Powerup[sizeX, sizeY];

            for (int x = 0; x < sizeX; x++ )
                for (int y = 0; y < sizeY; y++)
                {
                    _powerups[x, y] = Powerup.None;
                }

            GenerateTerrain();
            PrintTerrain();
        }

        private void GenerateTerrain()
        {
            Random rnd = new Random();

            // Iterate over cells
            for (int x = 0; x < _sizeX; x++)
            {
                for (int y = 0; y < _sizeY; y++)
                {
                    // There is a wall brick on every even cell
                    if ((x % 2 == 1) && (y % 2 == 1))
                    {
                        _tiles[x, y] = TileType.Wall;
                    }
                    else
                    {
                        // Randomly generate bricks
                        int num = rnd.Next(3);

                        if (num != 0 &&
                            !((x < 3 || x > _sizeX - 3) && (y < 3 || y > _sizeY - 3))) // Prevent bricks on starting position
                        {
                            _tiles[x, y] = TileType.Brick;
                        }
                        else
                        {
                            _tiles[x, y] = TileType.Grass;
                        }
                    }
                }
            }

            Console.WriteLine("Map generation done.");
        }

        public Point[] GetDestroyedBricksAndPlayers(Bomb bomb, List<Player> players, out List<Player> deadPlayers)
        {
            deadPlayers = new List<Player>();
            Dictionary<PlayerColors, Player> deadPlayerMap = new Dictionary<PlayerColors, Player>();


            List<Point> destroyedBricks = new List<Point>();

            int rangeCounter = bomb.Range + 1;

            int x = bomb.Position.X;
            int y = bomb.Position.Y;

            // UP FROM BOMB
            do
            {
                if (_tiles[x, y] == TileType.Brick)
                {
                    destroyedBricks.Add(new Point(x, y));
                    _tiles[x, y] = TileType.Grass;
                    break;
                }
                else if (_tiles[x, y] == TileType.Wall)
                    break;

                HandleDeadPlayers(new Point(x, y), players, ref deadPlayerMap);

                rangeCounter--;
                y--;
            } 
            while (rangeCounter > 0 && y >= 0);

            // DOWN FROM BOMB
            x = bomb.Position.X;
            y = bomb.Position.Y;

            rangeCounter = bomb.Range + 1;

            do
            {
                if (_tiles[x, y] == TileType.Brick)
                {
                    destroyedBricks.Add(new Point(x, y));
                    _tiles[x, y] = TileType.Grass;
                    break;
                }
                else if (_tiles[x, y] == TileType.Wall)
                    break;

                HandleDeadPlayers(new Point(x, y), players, ref deadPlayerMap);

                rangeCounter--;
                y++;
            }
            while (rangeCounter > 0 && y < SizeY);

            // LEFT FROM BOMB
            x = bomb.Position.X;
            y = bomb.Position.Y;

            rangeCounter = bomb.Range + 1;

            do
            {
                if (_tiles[x, y] == TileType.Brick)
                {
                    destroyedBricks.Add(new Point(x, y));
                    _tiles[x, y] = TileType.Grass;
                    break;
                }
                else if (_tiles[x, y] == TileType.Wall)
                    break;

                HandleDeadPlayers(new Point(x, y), players, ref deadPlayerMap);

                rangeCounter--;
                x--;
            }
            while (rangeCounter > 0 && x >= 0);

            // RIGHT FROM BOMB
            x = bomb.Position.X;
            y = bomb.Position.Y;

            rangeCounter = bomb.Range + 1;

            do
            {
                if (_tiles[x, y] == TileType.Brick)
                {
                    destroyedBricks.Add(new Point(x, y));
                    _tiles[x, y] = TileType.Grass;
                    break;
                }
                else if (_tiles[x, y] == TileType.Wall)
                    break;

                HandleDeadPlayers(new Point(x, y), players, ref deadPlayerMap);

                rangeCounter--;
                x++;
            }
            while (rangeCounter > 0 && x < SizeX);


            deadPlayers.AddRange(deadPlayerMap.Values);

            // Check done, return results
            Console.WriteLine("Explosion check, found destroyed bricks: " + destroyedBricks.Count + " dead players: " + deadPlayers.Count);

            return destroyedBricks.ToArray();
        }

        private void HandleDeadPlayers(Point tile, List<Player> players, ref Dictionary<PlayerColors, Player> deadPlayers)
        {
            // Check if there is a dead player there
            IEnumerable<Player> dead = players.Where(player => (player.GridPosition == tile));

            foreach (Player player in dead)
                deadPlayers[player.Color] = player;
        }

        public void SetPowerup(Point position, Powerup powerup)
        {
            _powerups[position.X, position.Y] = powerup;
        }

        public Powerup GetPowerup(Point position)
        {
            return _powerups[position.X, position.Y];
        }

        #region Interface
        [ProtoMember(1)]
        public int SizeX
        {
            get { return _sizeX;  }
            set { _sizeX = value; }
        }

        [ProtoMember(2)]
        public int SizeY
        {
            get { return _sizeY; }
            set { _sizeY = value; }
        }

        [ProtoMember(3)]
        public List<int> TileData
        {
            // Flatten 2D array into 1D for serialization
            get
            {
                List<int> data = new List<int>();

                for (int y = 0; y < SizeY; y++)
                {
                    for (int x = 0; x < SizeX; x++)
                    {
                        //data[x + SizeX*y] = (int)this._tiles[x,y];
                        data.Add((int)_tiles[x, y]);
                    }
                }

                return data;
            }

            // Unflatten 1D array to 2D when deserializing
            set
            {
                List<int> data = value;

                for (int x = 0; x < SizeX; x++)
                {
                    for (int y = 0; y < SizeY; y++)
                    {
                        _tiles[x, y] = (TileType)data[x + SizeX*y];
                    }
                }
            }
        }

        #endregion


        private void PrintTerrain()
        {
            Console.WriteLine("XXXXXXXXXXXXXXX");
            for (int y = 0; y < _tiles.GetUpperBound(1); y++)
            {
                Console.Write("X");
                for (int x = 0; x < _tiles.GetUpperBound(0); x++)
                {
                    switch(_tiles[x,y])
                    {
                        case TileType.Wall:
                            Console.Write("X");
                            break;
                        case TileType.Grass:
                            Console.Write(" ");
                            break;
                        case TileType.Brick:
                            Console.Write("*");
                            break;
                    }
                }

                Console.WriteLine("X");
            }

            Console.WriteLine("XXXXXXXXXXXXXXX");
        }

        public void Serialize(MemoryStream ms)
        {
            ms.WriteByte((byte)ServerMessageTypes.Map);
            Serializer.SerializeWithLengthPrefix(ms, this, PrefixStyle.Base128);
        }
    }
}
