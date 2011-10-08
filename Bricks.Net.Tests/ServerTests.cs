using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NUnit.Framework;

namespace Bricks.Net.Tests
{
    [TestFixture]
    class ServerTests
    {
        [Test]
        public void ServerRemovesStaleConnections()
        {
            ServerHelper.Server((server, port, sync) =>
            {
                var socket = new TcpSocket(TcpSocket.TcpSocketType.IPv4, false);
                socket.Connect(port, null, self => {
                    Assert.AreEqual(1, server.ConnectionCount);
                    self.End();
                    Thread.Sleep(500);
                    Assert.AreEqual(0, server.ConnectionCount);
                });
                            
            });
        }

        [Test]
        public void PauseShouldPreventNewConnections()
        {
            ServerHelper.Server((server, port, sync) =>
            {
                var start = DateTime.Now;
                server.Pause(20000); // prevent connections for 20 seconds

                var socket = new TcpSocket(TcpSocket.TcpSocketType.IPv4, false);
                socket.Connect(port, null, self =>
                {
                    Assert.IsTrue((DateTime.Now - start).TotalSeconds > 20) ;
                    sync.Set();
                });

                Thread.Sleep(20000);
                sync.Set(); // we never connected
            });
        }
    }
}
