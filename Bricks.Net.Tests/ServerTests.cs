using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NUnit.Framework;

namespace Bricks.Net.Tests
{
    [TestFixture]
    public class ServerTests
    {
        [Test]
        public void ServerRemovesStaleConnections()
        {
            ServerHelper.Server((server, port, sync) =>
            {
                var socket = new TcpSocket(TcpSocketType.IPv4);
                socket.Connect(port, connectedCallback: self =>
                {
                    Assert.AreEqual(1, server.ConnectionCount);
                    self.Dispose();
                    Thread.Sleep(500);
                    Assert.AreEqual(0, server.ConnectionCount);

                    sync.SignalAndWait();
                });                            
            });
        }

        [Test]
        public void PauseShouldPreventNewConnections()
        {
            ServerHelper.EchoServer((server, port, sync) =>
            {
                server.Pause(5000); // prevent connections for 5 seconds

                Assert.IsTrue(server.IsPaused, "Server should be paused");
                var socket = new TcpSocket(TcpSocketType.IPv4);
                socket.Data += (data, count) => Assert.Fail("We should never receive data");
                socket.Connect(port, connectedCallback: self =>
                {
                    Assert.IsTrue(server.IsPaused, "Server should be paused");
                    self.Write("test");

                    Thread.Sleep(2000);
                    Assert.IsFalse(self.IsConnected);

                    sync.SignalAndWait();
                });

                Thread.Sleep(10000);

                Assert.IsFalse(server.IsPaused, "Server should be unpaused");

                //accepting connections
                socket = new TcpSocket(TcpSocketType.IPv4);
                socket.Connect(port, connectedCallback: self =>
                {
                    Assert.IsTrue(self.IsConnected);

                    sync.SignalAndWait();
                });
            }, 3);
        }

        [Test]
        public void CallingPauseASecondTimeShouldDoNothing()
        {
            ServerHelper.EchoServer((server, port, sync) =>
            {
                server.Pause(5000); // prevent connections for 5 seconds

                Assert.IsTrue(server.IsPaused, "Server should be paused");
                Thread.Sleep(1000); //should have about 4 seconds left
                server.Pause(10000); // if we had a failure pause would in theory reset to 10s
                Thread.Sleep(5000);
                Assert.IsFalse(server.IsPaused, "Server should be paused"); //we should be a 6s which is 4s less then 10
            }, 1);
        }
    }
}
