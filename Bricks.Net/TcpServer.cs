using System;
using System.Net;
using System.Net.Sockets;
using NetSocket = System.Net.Sockets.Socket;

using System.Threading;

namespace Bricks.Net
{
    public class TcpServer
    {
        private TcpListener listener;
        private Thread listeningThread;

        public bool Bind(IPEndPoint localEP)
        {
            if (this.listener != null)
                return false;

            try
            {
                this.listener = new TcpListener(localEP);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool Bind(IPAddress localaddr, int port)
        {
            if (this.listener != null)
                return false;

            try
            {
                this.listener = new TcpListener(localaddr, port);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool Listen(int backlog = 128)
        {
            if (this.listener == null)
                return false;

            try
            {
                this.listener.Start(backlog);
                this.listeningThread = new Thread(() =>
                {
                    while(this.listener != null)
                    {
                        var socket = this.listener.AcceptSocket();
                        this.OnConnection(socket);
                    }
                });
                this.listeningThread.Start();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public EventHandler<SocketEventArgs> Connection;
        protected virtual void OnConnection(NetSocket socket)
        {
            var handler = this.Connection;
            if (handler != null)
                handler(this, new SocketEventArgs(socket));
        }

        public void Close()
        {
            if (this.listener == null)
                return;

            this.listener.Stop();
            this.listener = null;
        }
    }

    public class SocketEventArgs : EventArgs
    {
        public NetSocket Socket { get; private set; }

        public SocketEventArgs(NetSocket socket)
        {
            this.Socket = socket;
        }
    }
}