using System.Collections.Generic;
using ProtoBuf;

namespace DynaBomberClient.Communication.ServerMsg
{
    public enum Powerup
    {
        None = 0,
        AdditionalBomb = 1,
        BombRange = 2,
        ScrambledControls = 3,
        ManualTrigger = 4
    }


    [ProtoContract]
    public class BombExplode
    {
        public BombExplode()
        {
            DestroyedBricks = new List<BrickPosition>();
        }

        [ProtoMember(1)]
        public int X { get; set; }
        [ProtoMember(2)]
        public int Y { get; set; }
        [ProtoMember(3)]
        public int Range { get; set; }

        [ProtoMember(4)]
        public List<BrickPosition> DestroyedBricks { get; set; }
    }

    [ProtoContract]
    public class BrickPosition
    {
        public BrickPosition()
        { }

        [ProtoMember(1)]
        public int X { get; set; }
        [ProtoMember(2)]
        public int Y { get; set; }
        [ProtoMember(3)]
        public Powerup SpawnedPowerup { get; set; }
    }
}
