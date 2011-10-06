using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using NetSocket = System.Net.Sockets.Socket;

namespace Bricks.Net
{
    public class Server
    {
        public int? MaxConnections { get; set; }

        public int ConnectionCount
        {
            get { return this._sockets.Count; }
        }

        private Socket _socket;
        private readonly List<TcpSocket> _sockets = new List<TcpSocket>();


        public Server(Action<TcpSocket> connectionListener = null)
        {
            if (connectionListener != null)
                this.Connection += connectionListener;
        }

        public void Listen(int port, string host, Action<Server> callback)
        {
            this._socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //TODO: parse the host value properly
            //TODO: investigate how long this takes it might make sense to load it all into another thread
            this._socket.Bind(new IPEndPoint(IPAddress.Any, port));
            this._socket.Listen(100);
            this._socket.BeginAccept(EndListen, this);

            callback.TryInvoke(this);
        }

        private void EndListen(IAsyncResult ar)
        {
            var incoming = this._socket.EndAccept(ar);

            var socket = new TcpSocket(incoming);
            this._sockets.Add(socket);
            //TODO: we need something to cleanup stale sockets
            this.OnConnection(socket);
            this._socket.BeginAccept(EndListen, this);
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
            this.Connection.TryInvoke(socket);
        }

        public event Action Listening;
        protected virtual void OnListening()
        {
            this.Listening.TryInvoke();
        }

        public event Action Closed;
        protected virtual void OnClosed()
        {
            this.Closed.TryInvoke();
        }

        public event Action<Exception> Error;
        protected virtual void OnError(Exception exception)
        {
            this.Error.TryInvoke(exception);
        }

    }
}
