using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Bricks.Net;

namespace TestApp
{
    class Program
    {
        private static Server echoServer;

        static void Main(string[] args)
        {
            var sync = new ManualResetEventSlim();
            var hello = Encoding.UTF8.GetBytes("hello");
            echoServer = new Server(OnConnection);

            echoServer.Listen(1234, null, null);

            
            var sender = new TcpSocket(TcpSocket.TcpSocketType.IPv4, false);
            sender.Connect(1234, null, self => self.Write(hello));
            sender.Data += (data, count) =>
            {
                if("hello" != Encoding.UTF8.GetString(data, 0, count))
                    throw new Exception();

                sync.Set();//we can exit the main thread now
            };
            

            sync.Wait();
        }

        static void OnConnection(TcpSocket socket)
        {
            socket.Data += (data, count) => socket.Write(data);
        }
    }
}
