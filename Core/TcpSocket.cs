using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Net;

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

        public void Connect(int port, string host = null, Action<TcpSocket> callback = null)
        {


            _socket.BeginConnect(this.RemoteEndPoint, ar => { ar.IsCompleted }, null);
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public override void DestroySoon()
        {
            throw new NotImplementedException();
        }

        public override bool Write(byte[] chunk)
        {
            throw new NotImplementedException();
        }

        public override bool Write(string chunk, Encoding encoding = null)
        {
            throw new NotImplementedException();
        }

        public override void End()
        {
            throw new NotImplementedException();
        }

        public override void End(byte[] chunk)
        {
            throw new NotImplementedException();
        }

        public override void End(string chunk, Encoding encoding = null)
        {
            throw new NotImplementedException();
        }
    }
}
