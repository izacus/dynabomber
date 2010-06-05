using System.IO;

namespace DynaBomberClient.Communication.ClientMsg
{
    public interface IClientMessage
    {
        void Serialize(MemoryStream ms);
    }
}
