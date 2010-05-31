using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ProtoBuf;

namespace DynaBomber_Server.Interop
{
    [ProtoContract]
    public class BombExplosion : IUpdate
    {
        public BombExplosion()
        {}

        public BombExplosion(int x, int y, int range, BrickPosition[] bricks)
        {
            this.X = x;
            this.Y = y;
            this.Range = range;

            this.DestroyedBricks = bricks.ToList();
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
            ms.WriteByte((byte)MessageType.BombExplosion);
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
            this.X = pos.X;
            this.Y = pos.Y;
            this.SpawnedPowerup = spawnedPowerup;
        }

        [ProtoMember(1)]
        public int X { get; set; }
        [ProtoMember(2)]
        public int Y { get; set; }
        [ProtoMember(3)]
        public Powerup SpawnedPowerup { get; set; }

    }
}
