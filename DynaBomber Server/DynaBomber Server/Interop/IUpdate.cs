using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DynaBomber_Server.Interop
{
    public interface IUpdate
    {
        void Serialize(MemoryStream ms);
    }
}
