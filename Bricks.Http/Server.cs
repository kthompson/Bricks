using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bricks.Net;

namespace Bricks.Http
{
    public class Server
    {
        #region events
        public event Action<ServerRequest, ServerResponse> Request;
        protected virtual void OnRequest(ServerRequest req, ServerResponse res)
        {
            var handler = this.Request;
            if (handler != null)
                handler(req, res);
        }

        public event Action<TcpSocket> Connection;
        protected virtual void OnConnection(TcpSocket socket)
        {
            var handler = this.Connection;
            if (handler != null)
                handler(socket);
        }

        public event Action<int> Close;
        protected virtual void OnClose(int error)
        {
            var handler = this.Close;
            if (handler != null)
                handler(error);
        }

        public event Action<ServerRequest, ServerResponse> CheckContinue;
        protected virtual void OnCheckContinue(ServerRequest req, ServerResponse res)
        {
            var handler = this.CheckContinue;
            if (handler != null)
                handler(req, res);
        }

        public event Action<ServerRequest, TcpSocket, Stream> Upgrade;
        protected virtual void OnUpgrade(ServerRequest req, TcpSocket socket, Stream head)
        {
            var handler = this.Upgrade;
            if (handler != null)
                handler(req, socket, head);
        }

        public event Action<Exception> ClientError;
        protected virtual void OnClientError(Exception exception)
        {
            var handler = this.ClientError;
            if (handler != null)
                handler(exception);
        }
        #endregion

        public Server(Action<ServerRequest, ServerResponse> requestListener = null)
        {
            if (requestListener != null)
                this.Request += requestListener;
        }


        public void Listen(int port, string hostname = null, Action callback = null)
        {
            
        }

        public void StopListening()
        {
        }
    }
}
