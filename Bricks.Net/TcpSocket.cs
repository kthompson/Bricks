using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Bricks.Net
{

    public enum TcpSocketType
    {
        IPv4,
        IPv6
    }

    public sealed class TcpSocket : Stream
    {
        private static int _nextId;
        private int _id = _nextId++;

        private readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();

        public TcpSocket(TcpSocketType type)
            : this(type, false)
        {
        }

        private TcpSocket(TcpSocketType type, bool allowHalfOpen)
        {
            switch (type)
            {
                case TcpSocketType.IPv4:
                    _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    break;
                case TcpSocketType.IPv6:
                    _socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
            this.InitializeSocket();
        }

        internal TcpSocket(Socket socket)
        {
            this._socket = socket;
            this.InitializeSocket();

            StartTasks();
        }

        private CancellationTokenSource _tasksHandler = new CancellationTokenSource();
        private void StartTasks()
        {
            SetKeepAlive(1000, 1000);

            //Helper.StartLongRunningTask(this.ReceiveTask, _tasksHandler.Token);
            Helper.StartLongRunningTask(this.ReceiveDataTask, _tasksHandler.Token);
            Task.Factory.StartNew(this.ReceiveTask, _tasksHandler.Token);
        }

        private void InitializeSocket()
        {
            //this._socket.Blocking = false;
            this.BufferSize = 8192;
        }

        private bool SetKeepAlive(uint keepAliveTime, uint keepAliveInterval)
        {
            try
            {
                //_socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.KeepAlive, 1);//for enabling tcpkeepalive


                // resulting structure
                var keepAliveValues = new byte[12];
                uint keepAliveEnable = 1;

                
                if (keepAliveTime == 0 || keepAliveInterval == 0) 
                    keepAliveEnable = 0;

                // Bytes 00-03 are 'enable' where '1' is true, '0' is false
                // Bytes 04-07 are 'time' in milliseconds
                // Bytes 08-12 are 'interval' in milliseconds

                Array.Copy(BitConverter.GetBytes(keepAliveEnable), 0, keepAliveValues, 0, 4);
                Array.Copy(BitConverter.GetBytes(keepAliveTime), 0, keepAliveValues, 4, 4);
                Array.Copy(BitConverter.GetBytes(keepAliveInterval), 0, keepAliveValues, 8, 4);
                
                // create bytestruct for result (bytes pending on server socket)
                byte[] result = BitConverter.GetBytes(0);
                
                // write SIO_VALS to Socket IOControl
                _socket.IOControl(IOControlCode.KeepAliveValues, keepAliveValues, result);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        private void ReceiveTask()
        {
            if (this.IsDisposed)
                return;

            using (_rwLock.ReadLock())
            {
                // allocate a buffer
                var chunk = new byte[this.BufferSize];

                try
                {
                    var args = new SocketAsyncEventArgs();
                    args.SetBuffer(chunk, 0, chunk.Length);
                    args.Completed += (sender, e) =>
                    {
                        if(e.BytesTransferred == 0)
                        {
                            this.OnClose();
                            return;
                        }

                        var item = Tuple.Create(e.Buffer, e.BytesTransferred);
                        
                        lock (_dataQueue)
                        {
                            _dataQueue.Enqueue(item);
                            Monitor.Pulse(_dataQueue);
                        }
                        
                        this.BytesRead += e.BytesTransferred;
                        ReceiveTask();
                    };

                    this._socket.ReceiveAsync(args);
                }
                catch (SocketException e)
                {
                    //if we received a SocketException then don't trigger the ReceiveCallback
                    return;
                }
                catch (Exception e)
                {
                    e.ToString();
                    return;
                }
            }
        }

        private void ReceiveDataTask()
        {
            Tuple<byte[], int> item;
            while (!this.IsDisposed)
            {
                lock (_dataQueue)
                {
                    if (_dataQueue.Count == 0)
                        Monitor.Wait(_dataQueue);


                    if (_dataQueue.Count == 0)
                        //no items in queue but we were pulsed so we must be shutting down
                        return;

                    item = _dataQueue.Dequeue(); 
                }

                this.OnData(item.Item1, item.Item2);
            }
        }

        private Queue<Tuple<byte[], int>> _dataQueue = new Queue<Tuple<byte[], int>>();

        private Socket _socket;

        public int BufferSize
        {
            get
            {
                return this._socket.ReceiveBufferSize;
            }
            set
            {
                this._socket.ReceiveBufferSize = value;
                this._socket.SendBufferSize = value;
            }
        }

        public IPEndPoint EndPoint
        {
            get { return (IPEndPoint)this._socket.LocalEndPoint; }
        }

        public IPEndPoint RemoteEndPoint
        {
            get
            {
                if (!this.IsConnected)
                {
                    return null;
                }

                return (IPEndPoint)this._socket.RemoteEndPoint;
            }
        }

        public bool IsConnected
        {
            get
            {
                return !this.IsDisposed && this._socket.Connected;
            }
        }



        public int BytesRead { get; private set; }
        public int BytesWritten { get; private set; }

        public bool NoDelay
        {
            get { return this._socket.NoDelay; }
            set { this._socket.NoDelay = value; }
        }

        public void Connect(int port, IPAddress address, Action<TcpSocket> connectedCallback = null)
        {
            var endPoint = new IPEndPoint(address, port);
            Connect(endPoint, connectedCallback);
        }

        public void Connect(int port, string host = "localhost", Action<TcpSocket> connectedCallback = null)
        {
            var endPoint = new DnsEndPoint(host, port);
            Connect(endPoint, connectedCallback);
        }

        private void Connect(EndPoint endPoint, Action<TcpSocket> connectedCallback)
        {
            this.Connected += connectedCallback;

            var args = new SocketAsyncEventArgs {RemoteEndPoint = endPoint};
            args.Completed += (sender, e) =>
            {
                this.StartTasks();
                this.OnConnected();
            };
            this._socket.ConnectAsync(args);
        }

        public void SetTimeout(int timeout, Action<TcpSocket> callback = null)
        {
            throw new NotImplementedException();
        }

        public override void Pause()
        {
            throw new NotImplementedException();
        }

        public override void Resume()
        {
            throw new NotImplementedException();
        }

        public override void Destroy()
        {
            this._socket.Shutdown(SocketShutdown.Both);
        }

        public override bool Write(byte[] chunk, int index, int count, Action dataWrittenCallback = null)
        {
            if (this.IsDisposed)
                return false;

            var args = new SocketAsyncEventArgs();
            args.SetBuffer(chunk, index, count);
            args.Completed += (sender, e) =>
                                  {
                                      this.BytesWritten += e.BytesTransferred;
                                      dataWrittenCallback.TryInvoke();
                                  };


            return this._socket.SendAsync(args);
        }

        public override void End()
        {
            this._socket.Shutdown(SocketShutdown.Send);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) 
                return;

            lock (_dataQueue)
            {
                //pulse the ReceiveDataTask so it exits out
                Monitor.Pulse(_dataQueue);   
            }

            using (_rwLock.WriteLock())
            {
                if (_tasksHandler != null)
                {
                    _tasksHandler.Cancel();
                    _tasksHandler.Dispose();
                    _tasksHandler = null;
                }

                if (this._socket == null)
                    return;

                this._socket.Shutdown(SocketShutdown.Both);
                this._socket.Dispose();
                this._socket = null;
            }
        }

        protected override void OnClose()
        {
            base.OnClose();
            this.Dispose(true);
        }

        public event Action<TcpSocket> Connected;
        private void OnConnected()
        {
            this.Connected.TryInvoke(this);
        }
    }
}
