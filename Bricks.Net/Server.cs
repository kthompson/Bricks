using System;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.Net.Sockets;
using NetSocket = System.Net.Sockets.Socket;
using System.Threading.Tasks;
using System.Threading;

namespace Bricks.Net
{
    public class Server : IDisposable
    {
        public int? MaxConnections { get; set; }

        public int ConnectionCount
        {
            get
            {
                lock (_sockets)
                {
                    return this._sockets.Count;    
                }
            }
        }

        public bool IsPaused { get; private set; }

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
            
            this._socket.Bind(new DnsEndPoint(host ?? "localhost", port).ToIPEndPoint());
            this._socket.Listen(100);
			
			StartTasks();

            this.Listening += () => callback(this);
            this.Listening.TryInvoke();
        }

		private readonly CancellationTokenSource _tasksHandler = new CancellationTokenSource();
        private readonly ManualResetEventSlim _unpauseEvent = new ManualResetEventSlim(false);

		private void StartTasks ()
		{
            var token = _tasksHandler.Token;
            Helper.StartLongRunningTask(AcceptConnectionsTask, token);
            Helper.StartLongRunningTask(StaleSocketsCleaner, token);
		}

        private void StaleSocketsCleaner()
        {
            while (!_tasksHandler.IsCancellationRequested)
            {
                lock (_sockets)
                {
                    var socket = _sockets.FirstOrDefault(s => !s.IsConnected);
                    if(socket != null)
                    {
                        _sockets.Remove(socket);
                    }
                }

                Thread.Sleep(100);
            }
        }

        private void AcceptConnectionsTask()
        {
            while (!_tasksHandler.IsCancellationRequested)
            {
                Socket incoming;
                try
                {
                    incoming = this._socket.Accept();
                }
                catch(SocketException)
                {
                    if (this._socket == null) //we got disposed
                    {
                        OnClosed();
                        return;
                    }

                    throw;
                }

                if (this.IsPaused)
                {
                    //incoming.Disconnect(false);
                    //incoming.Close();
                    //incoming.Dispose();
                    incoming.Shutdown(SocketShutdown.Both);

                    _unpauseEvent.Wait();
                    _unpauseEvent.Reset(); //next wait will block
                    continue;
                }

                var socket = new TcpSocket(incoming);
                lock (_sockets)
                {
                    this._sockets.Add(socket);    
                }

                this.OnConnection(socket);
                Thread.Sleep(0); //allow any ThreadInterruptedExceptions to bubble
            }
        }

        /// <summary>
        /// Stop accepting connections for <param name="ms">ms</param> milliseconds
        /// </summary>
        /// <param name="ms"></param>
        public void Pause(int ms = 100)
        {
            //already pausing or we are not listening yet
            if (this.IsPaused)
                return;

            this.IsPaused = true;

            //TODO: the only way to make this better would be Dispose and recreate the Socket 

            /*

            //start a new connection to ourselves to break the _socket.Accept() in AcceptConnectionsTask

            var socketClosed = new ManualResetEventSlim();
            var socket = new TcpSocket(this.GetTcpSocketType());

            //block until we can confirm we have been closed
            socket.Close += () => socketClosed.Set();

            var ep = (IPEndPoint) this._socket.LocalEndPoint;
            var addr = ep.Address.ToString() == "0.0.0.0" ? IPAddress.Loopback : ep.Address;

            socket.Connect(ep.Port, addr); // on connect immediately dispose
            socketClosed.Wait();
             
            */

            //start are sleeper task and unpause once its complete
            var sleeper = new Task(() => Thread.Sleep(ms));
            sleeper.ContinueWith(task =>
                                     {
                                         this.IsPaused = false;
                                         _unpauseEvent.Set(); //notify waiting Task that we have unpaused
                                     });
			sleeper.Start();
        }

        private TcpSocketType GetTcpSocketType()
        {
            switch (this._socket.LocalEndPoint.AddressFamily)
            {
                case AddressFamily.InterNetworkV6:
                    return TcpSocketType.IPv6;
                default:
                    return TcpSocketType.IPv4;
            }
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

        private bool _disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) 
                return;
            
            lock (_sockets)
            {
                foreach (var socket in _sockets)
                {
                    socket.Dispose();
                }    

                _sockets.Clear();
            }

            this._tasksHandler.Cancel();
            this._tasksHandler.Dispose();

            if (this._socket != null)
            {
                this._socket.Dispose();
                this._socket = null;
            }
            this._disposed = true;
        }

        public void Dispose()
        {
            this.Dispose(true);
        }
    }
}
