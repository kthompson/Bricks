using System;

namespace Bricks.Net
{
    public class TcpSocketEventArgs : EventArgs
    {
        public TcpSocket Socket { get; private set; }

        public TcpSocketEventArgs(TcpSocket socket)
        {
            this.Socket = socket;
        }
    }
}