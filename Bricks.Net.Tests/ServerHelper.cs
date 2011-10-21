using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using NUnit.Framework;

namespace Bricks.Net.Tests
{
    public class ServerHelper
    {
        private static int _nextPort = 1234;
        private static readonly object _lock = new object();

        static ServerHelper()
        {
            _nextPort = new Random().Next(1234, short.MaxValue/2);
        }

        private static int GetPort()
        {
            lock (_lock)
            {
                return _nextPort++;    
            }
        }

        public static void EchoServer(Action<Server, int, Barrier> onConnect, int count = 2, int timeout = 10)
        {
            var sync = new Barrier(count);
            var port = ServerHelper.GetPort();
            using (var echoServer = new Server(socket => socket.Data += (data, size) => socket.Write(data, 0, size)))
            {
                echoServer.Listen(port, null, server => onConnect(server, port, sync));

                if (timeout == 0 || Debugger.IsAttached)
                {
                    sync.SignalAndWait();
                    return;
                }

                Assert.IsTrue(sync.SignalAndWait(TimeSpan.FromSeconds(timeout)), "Failed to call Set");
            }
        }

		public static void Server(Action<Server, int, Barrier> onListen, int count = 2, int timeout = 10)
        {
            var sync = new Barrier(count);
            var port = ServerHelper.GetPort();
            using (var server = new Server())
            {
                server.Listen(port, null, s => onListen(server, port, sync));

                Assert.IsTrue(sync.SignalAndWait(TimeSpan.FromSeconds(timeout)), "Failed to call Set");
            }
        }
    }
}
