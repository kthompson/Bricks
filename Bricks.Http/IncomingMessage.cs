using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bricks.Net;

namespace Bricks.Http
{
    /// <summary>
    /// Abstract base class for ServerRequest and ClientResponse
    /// </summary>

    public abstract class IncomingMessage : Stream
    {
        protected IncomingMessage(TcpSocket socket)
        {
            this.Connection = socket;

            this.HttpVersion = null;
            this.Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            this.Trailers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            this.Readable = true;

            this.Method = null;
        }

         #region events



        
        #endregion


        public string Method { get; private set; }
        public Dictionary<string, string> Headers { get; private set; }
        public Dictionary<string, string> Trailers { get; private set; }
        public string HttpVersion { get; private set; }
        public TcpSocket Connection { get; private set; }

        public override void Pause()
        {
            
        }

        public override void Resume()
        {
        }
    }
}
