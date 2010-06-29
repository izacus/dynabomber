using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using ProtoBuf;

namespace DynaBomberClient.Communication.ServerMsg
{
    [ProtoContract]
    public class ServerResponse
    {
        public enum Response
        {
            JoinOk
        }

        public ServerResponse()
        {}

        [ProtoMember(1)]
        public Response Value { get; set; }
    }
}
