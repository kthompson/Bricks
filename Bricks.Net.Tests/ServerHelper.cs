using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NUnit.Framework;

namespace Bricks.Net.Tests
{
    public class ServerHelper
    {
        private static int _nextPort = 1234;
        public static int GetPort()
        {
            return _nextPort++;
        }

        public static void EchoServer(Action<Server, int, ManualResetEventSlim> onConnect, int timeout = 10)
        {
            var sync = new ManualResetEventSlim();
            var port = ServerHelper.GetPort();
            var echoServer = new Server(socket => socket.Data += (data, count) => socket.Write(data, 0, count));
            echoServer.Listen(port, null, server => onConnect(server, port, sync));

            Assert.IsTrue(sync.Wait(TimeSpan.FromSeconds(timeout)), "Failed to call Set");
        }

        public static void Server(Action<Server, int, ManualResetEventSlim> onConnect, int timeout = 10)
        {
            var sync = new ManualResetEventSlim();
            var port = ServerHelper.GetPort();
            var echoServer = new Server();
            echoServer.Listen(port, null, server => onConnect(server, port, sync));

            Assert.IsTrue(sync.Wait(TimeSpan.FromSeconds(timeout)), "Failed to call Set");
        }
    }
}
