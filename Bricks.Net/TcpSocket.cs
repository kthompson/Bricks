using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Net;
using System.Threading;

namespace Bricks.Net
{
    public class TcpSocket : Stream
    {
        //private ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        public enum TcpSocketType
        {
            IPv4,
            IPv6
        }

        public TcpSocket(TcpSocketType type, bool allowHalfOpen)
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

            ThreadPool.QueueUserWorkItem(o => this.BeginReceive());
        }

        private void InitializeSocket()
        {
            //this._socket.Blocking = false;
            this.BufferSize = 4096;
        }

        protected void BeginReceive()
        {
            try
            {
                //this._rwLock.EnterReadLock();

                // allocate a buffer
                var chunk = new byte[this.BufferSize];

                // kick off an async read
                this._socket.BeginReceive(chunk, 0, chunk.Length, SocketFlags.None, EndReceive, chunk);

            }
            finally
            {
                //this._rwLock.ExitReadLock();
            }
        }

        private void EndReceive(IAsyncResult ar)
        {
                 
            byte[] buffer;
            SocketError errorCode;
            int size;

            try
            {
                //this._rwLock.EnterReadLock();

                //immediately start the next receive
                this.BeginReceive();

                //get the buffer created for this receive
                buffer = (byte[])ar.AsyncState;

                //get the data length received
                size = this._socket.EndReceive(ar, out errorCode);

            }
            catch (SocketException)
            {
                //if we received a SocketException then don't trigger the ReceiveCallback
                return;
            }
            catch(Exception e)
            {
                return;       
            }
            finally
            {
                //this._rwLock.ExitReadLock();
            }

            switch (errorCode)
            {
                case SocketError.Success:
                    if(size > 0)
                        this.OnData(buffer, size);
                    break;
                default:
                    this.OnError((int) errorCode);
                    break;
            }
        }

        private readonly Socket _socket;

        public int BufferSize { get; set; }
        public IPEndPoint EndPoint { get; private set; }
        public IPEndPoint RemoteEndPoint { get; private set; }
        public int BytesRead { get; private set; }
        public int BytesWritten { get; private set; }

        public bool NoDelay
        {
            get { return this._socket.NoDelay; }
            set { this._socket.NoDelay = value; }
        }

        public void Connect(int port, string host = null, Action<TcpSocket> connectedCallback = null)
        {
            if (string.IsNullOrEmpty(host))
            {
                this.RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, port);
            }
            else
            {
                var ip = Dns.GetHostAddresses(host).FirstOrDefault();
                if (ip == null)
                    throw new ArgumentException("host could not be resolved to a valid address", "host");

                this.RemoteEndPoint = new IPEndPoint(ip, port);
            }
            this.Connected += connectedCallback;

            _socket.BeginConnect(this.RemoteEndPoint, EndConnect, _socket);
        }

        private void EndConnect(IAsyncResult ar)
        {
            _socket.EndConnect(ar);

            this.OnConnected();
            this.BeginReceive();
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
            this._socket.Close();
        }

        public override void DestroySoon()
        {
            throw new NotImplementedException();
        }

        //private readonly Queue<Tuple<byte[], Action>> _packets = new Queue<Tuple<byte[], Action>>();
        public bool Write(byte[] chunk, Action dataWrittenCallback)
        {
            try
            {
                //this._rwLock.EnterWriteLock();
                //lock (this._packets)
                //{
                //if (this._socket.Blocking || this._packets.Count > 0)
                //{

                //    _packets.Enqueue(Tuple.Create(chunk, dataWrittenCallback));
                //    ThreadPool.QueueUserWorkItem(state => WritePacketFromQueue());
                //    return false;
                //}
                //TODO: do we need to be throttling this?
                _socket.BeginSend(chunk, 0, chunk.Length, SocketFlags.None, ar => EndWrite(ar, dataWrittenCallback), this._socket);
                //}
            }
            finally
            {
                //this._rwLock.ExitWriteLock();
            }

            return false;
        }

        private void EndWrite(IAsyncResult ar, Action dataWrittenCallback)
        {
            try
            {
                //this._rwLock.EnterWriteLock();

                this._socket.EndSend(ar);
            }
            finally
            {
                //this._rwLock.ExitWriteLock();
            }

            dataWrittenCallback.TryInvoke();
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

        public override bool Write(byte[] chunk)
        {
            return this.Write(chunk, null);
        }

        public bool Write(string chunk, Encoding encoding = null, Action dataWrittenCallback = null)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;

            return this.Write(encoding.GetBytes(chunk), dataWrittenCallback);
        }

        public override void End()
        {
            this._socket.Shutdown(SocketShutdown.Both);
        }

        public event Action<TcpSocket> Connected;
        protected virtual void OnConnected()
        {
            this.Connected.TryInvoke(this);
        }
    }
}
