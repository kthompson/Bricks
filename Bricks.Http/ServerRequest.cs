using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bricks.Net;

namespace Bricks.Http
{
    public class ServerRequest : IncomingMessage
    {
        public ServerRequest(TcpSocket socket)
            : base(socket)
        {
            this.Url = string.Empty;
        }

        public string Url { get; private set; }
    }
}
