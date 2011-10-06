using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NUnit.Framework;

namespace Bricks.Net.Tests
{
    [TestFixture]
    class APITests
    {
        [Test]
        public void EchoServerTest()
        {
            var sync = new ManualResetEventSlim();
            var hello = Encoding.UTF8.GetBytes("hello");
            var echoServer = new Server(socket => socket.Data += (data, count) => socket.Write(data, 0, count));
            echoServer.Listen(1234, null, server =>
                                              {
                                                  var sender = new TcpSocket(TcpSocket.TcpSocketType.IPv4, false);
                                                  sender.Connect(1234, null, self => self.Write(hello));
                                                  sender.Data += (data, count) =>
                                                  {
                                                      Assert.AreEqual("hello", Encoding.UTF8.GetString(data, 0, count));
                                                      sync.Set();//we can exit the main thread now
                                                  };
                                              });

            Assert.IsTrue(sync.Wait(TimeSpan.FromSeconds(10)), "Failed to call set");
        }
    }
}
