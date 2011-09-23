using System;
using System.Net;
using System.Net.Sockets;
using NetSocket = System.Net.Sockets.Socket;

namespace Bricks.Net
{
    public class Server
    {
        public int? MaxConnections { get; set; }
        public int ConnectionCount { get; internal set; }

        public Server(Action<TcpSocket> connectionListener = null)
        {
            if (connectionListener != null)
                this.Connection += connectionListener;
        }

        public void Listen(int port, string host, Action<Server> callback)
        {
            
        }

        /// <summary>
        /// Stop accepting connections for <param name="ms">ms</param> milliseconds
        /// </summary>
        /// <param name="ms"></param>
        public void Pause(int ms)
        {
            
        }

        public void Close()
        {
        }



        public event Action<TcpSocket> Connection;
        protected virtual void OnConnection(TcpSocket socket)
        {
            var handler = this.Connection;
            if (handler != null)
                handler(socket);
        }

        public event Action Listening;
        protected virtual void OnListening()
        {
            var handler = this.Listening;
            if (handler != null)
                handler();
        }

        public event Action Closed;
        protected virtual void OnClosed()
        {
            var handler = this.Closed;
            if (handler != null)
                handler();
        }

        public event Action<Exception> Error;
        protected virtual void OnError(Exception exception)
        {
            var handler = this.Error;
            if (handler != null)
                handler(exception);
        }

    }
}
