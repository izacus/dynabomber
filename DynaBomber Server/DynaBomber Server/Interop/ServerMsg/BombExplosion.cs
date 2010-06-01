using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProtoBuf;

namespace DynaBomber_Server.Interop.ServerMsg
{
    [ProtoContract]
    public class BombExplosion : IServerUpdate
    {
        public BombExplosion()
        {}

        public BombExplosion(int x, int y, int range, BrickPosition[] bricks)
        {
            X = x;
            Y = y;
            Range = range;

            DestroyedBricks = bricks.ToList();
        }

        [ProtoMember(1)]
        public int X { get; set; }
        [ProtoMember(2)]
        public int Y { get; set; }
        [ProtoMember(3)]
        public int Range { get; set; }

        [ProtoMember(4)]
        public List<BrickPosition> DestroyedBricks { get; set; }

        public void Serialize(MemoryStream ms)
        {
            ms.WriteByte((byte)ServerMessageTypes.BombExplosion);
            Serializer.SerializeWithLengthPrefix(ms, this, PrefixStyle.Base128);
        }
    }

    [ProtoContract]
    public class BrickPosition
    {
        public BrickPosition()
        {}

        public BrickPosition(Point pos, Powerup spawnedPowerup)
        {
            X = pos.X;
            Y = pos.Y;
            SpawnedPowerup = spawnedPowerup;
        }

        [ProtoMember(1)]
        public int X { get; set; }
        [ProtoMember(2)]
        public int Y { get; set; }
        [ProtoMember(3)]
        public Powerup SpawnedPowerup { get; set; }

    }
}
