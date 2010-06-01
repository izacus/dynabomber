using System.IO;

namespace DynaBomber_Server.Interop.ServerMsg
{
    public interface IServerUpdate
    {
        void Serialize(MemoryStream ms);
    }
}
