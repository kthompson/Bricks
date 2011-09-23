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

        public void Connect(int port, string host = null, Action<TcpSocket> callback = null)
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
            
            _socket.BeginConnect(this.RemoteEndPoint, ar =>
                                                          {
                                                              _socket.EndConnect(ar);
                                                              if (callback != null)
                                                                  callback(this);
                                                          }, _socket);
            
        }

        public void SetTimeout(int timeout, Action<TcpSocket> callback = null)
        {
            
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

        private readonly Queue<Tuple<byte[], Action>> _packets = new Queue<Tuple<byte[], Action>>();
        public bool Write(byte[] chunk, Action dataWrittenCallback)
        {
            lock (this._packets)
            {
                if (this._socket.Blocking || this._packets.Count > 0)
                {

                    _packets.Enqueue(Tuple.Create(chunk, dataWrittenCallback));
                    ThreadPool.QueueUserWorkItem(state => WritePacketFromQueue());
                    return false;
                }

                _socket.
                _socket.BeginSend(chunk, 0, chunk.Length, SocketFlags.None, ar => EndWrite(ar, dataWrittenCallback), this._socket);
            }

            return true;
        }

        private void EndWrite(IAsyncResult ar, Action dataWrittenCallback)
        {
            this._socket.EndSend(ar);

            if(dataWrittenCallback != null)
                dataWrittenCallback();
        }

        private void WritePacketFromQueue()
        {
            Tuple<byte[], Action> tuple;
            lock (this._packets)
            {
                tuple = _packets.Dequeue();
                while (this._socket.Blocking)
                {
                    Thread.Sleep(30);
                }

                _socket.Send(tuple.Item1, 0, tuple.Item1.Length, SocketFlags.None);

            }

            if (tuple.Item2 != null)
                tuple.Item2();
        }

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
    }
}
