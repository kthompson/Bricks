using System;
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
        private bool _closing;


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

        private readonly CancellationTokenSource _tasksHandler = new CancellationTokenSource();
        private void StartTasks()
        {
            SetKeepAlive(1000, 1000);

            Helper.StartLongRunningTask(this.ReceiveTask, _tasksHandler.Token);
            //Task.Factory.StartNew(this.HeartBeatTask, TaskCreationOptions.LongRunning);
        }

        private void InitializeSocket()
        {
            //this._socket.Blocking = false;
            this.BufferSize = 4096;
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

        private void HeartBeatTask()
        {
            while (!this.IsDisposed)
            {
                using (_rwLock.WriteLock())
                {
                    bool blockingState = _socket.Blocking;
                    try
                    {

                        var tmp = new byte[1];
                        _socket.Blocking = false;
                        _socket.Send(tmp, 0, 0);
                    }
                    catch (SocketException e)
                    {
                        // 10035 == WSAEWOULDBLOCK
                        if (!e.NativeErrorCode.Equals(10035))
                        {
                            OnError(e.NativeErrorCode);
                            OnClose();
                        }
                    }
                    finally
                    {
                        _socket.Blocking = blockingState;
                    }
                }

                Thread.Sleep(100);
            }
        }

        private void ReceiveTask()
        {
            while (!this.IsDisposed)
            {
                using (_rwLock.ReadLock())
                {
                    // allocate a buffer
                    var chunk = new byte[this.BufferSize];

                    try
                    {
                        
                        this._socket.BeginReceive(chunk, 0, chunk.Length, SocketFlags.None, iar => EndReceive(iar, chunk), null);
                    }
                    catch (SocketException e)
                    {
                        //if we received a SocketException then don't trigger the ReceiveCallback
                        return;
                    }
                    catch (Exception e)
                    {
                        return;
                    }
                }
            }
        }

        private void EndReceive(IAsyncResult result, byte[] chunk)
        {
            using (_rwLock.ReadLock())
            {
                try
                {
                    SocketError errorCode;
                    int size = this._socket.EndReceive(result, out errorCode);
                    if (size == 0)
                    {
                        this.OnClose();
                        return;
                    }

                    this.BytesRead += size;

                    switch (errorCode)
                    {
                        case SocketError.Success:
                            if (size > 0)
                            {
                                Task.Factory.StartNew(() => this.OnData(chunk, size), _tasksHandler.Token);
                            }
                            break;
                        default:
                            Task.Factory.StartNew(() => this.OnError((int) errorCode), _tasksHandler.Token);
                            break;
                    }
                }
                catch (SocketException e)
                {
                    //if we received a SocketException then don't trigger the ReceiveCallback
                    return;
                }
                catch (Exception e)
                {
                    return;
                }
            }
        }

        private Socket _socket;

        public int BufferSize { get; set; }
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
                return this._socket.Connected && !_closing;
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
            this.Connected += connectedCallback;

            Task.Factory.StartNew(() => ConnectTask(port, address), _tasksHandler.Token);
            
        }

        public void Connect(int port, string host = "localhost", Action<TcpSocket> connectedCallback = null)
        {
            this.Connected += connectedCallback;

            Task.Factory.StartNew(() => ConnectTask(port, host));
        }

        private void ConnectTask(int port, string host)
        {
            try
            {
                _socket.Connect(host, port);
                this.StartTasks();
            }
            catch(Exception e)
            {
                Trace.Write(e.ToString());
            }

            this.OnConnected();
        }

        private void ConnectTask(int port, IPAddress address)
        {
            try
            {
                _socket.Connect(address, port);
                this.StartTasks();
            }
            catch (Exception e)
            {
                Trace.Write(e.ToString());
            }

            this.OnConnected();
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

        //private readonly Queue<Tuple<byte[], Action>> _packets = new Queue<Tuple<byte[], Action>>();
        public override bool Write(byte[] chunk, int index, int count, Action dataWrittenCallback = null)
        {
            if (_closing)
                return false;

            var t = new Task(() => WriteTask(chunk, index, count, dataWrittenCallback));
            t.RegisterAssertions();
            t.Start();

            return false;
        }

        private void WriteTask(byte[] chunk, int index, int count, Action dataWrittenCallback)
        {
            using (_rwLock.WriteLock())
            {

                //lock (this._packets)
                //{
                //if (this._socket.Blocking || this._packets.Count > 0)
                //{

                //    _packets.Enqueue(Tuple.Create(chunk, dataWrittenCallback));
                //    ThreadPool.QueueUserWorkItem(state => WritePacketFromQueue());
                //    return false;
                //}

                //TODO: Check parameters for validity
                //TODO: do we need to be throttling this?
                SocketError errorCode;

                this.BytesWritten += _socket.Send(chunk, index, count, SocketFlags.None, out errorCode);

                switch (errorCode)
                {
                    case SocketError.Success:
                        Task.Factory.StartNew(() => dataWrittenCallback.TryInvoke(), _tasksHandler.Token);
                        break;
                    default:
                        Task.Factory.StartNew(() => this.OnError((int) errorCode), _tasksHandler.Token);
                        break;
                }

                //}
            }
        }

        //private void WritePacketFromQueue()
        //{
        //    Tuple<byte[], Action> tuple;
        //    lock (this._packets)
        //    {
        //        tuple = _packets.Dequeue();
        //        while (this._socket.Blocking)
        //        {
        //            Thread.Sleep(30);
        //        }

        //        _socket.BeginSend(tuple.Item1, 0, tuple.Item1.Length, SocketFlags.None, ar => EndWrite(ar, tuple.Item2), this._socket);

        //        if(this._packets.Count > 0)
        //            ThreadPool.QueueUserWorkItem(state => WritePacketFromQueue());
        //    }
        //}

        public override void End()
        {
            this._socket.Shutdown(SocketShutdown.Send);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) 
                return;

            _tasksHandler.Cancel();
            _tasksHandler.Dispose();

            if (this._socket == null) 
                return;

            this._socket.Dispose();
            this._socket = null;
        }

        protected override void OnClose()
        {
            base.OnClose();
            this._socket.Shutdown(SocketShutdown.Both);
            _closing = true;
        }

        public event Action<TcpSocket> Connected;
        private void OnConnected()
        {
            this.Connected.TryInvoke(this);
        }
    }
}
