using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bricks.Net;

namespace Bricks.Http
{
    abstract class ClientResponse : IncomingMessage
    {
        public ClientResponse(TcpSocket socket)
            : base(socket)
        {
        }
    }
}
