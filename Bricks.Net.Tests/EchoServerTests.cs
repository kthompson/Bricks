using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NUnit.Framework;

namespace Bricks.Net.Tests
{
    [TestFixture]
    class EchoServerTests
    {
        [Test]
        public void CanWriteString()
        {
            ServerHelper.EchoServer((server, port, sync) =>
            {
                var socket = new TcpSocket(TcpSocket.TcpSocketType.IPv4, false);
                socket.Connect(port, null, self => self.Write("hello"));
                socket.Data += (data, count) =>
                {
                    Assert.AreEqual("hello", Encoding.UTF8.GetString(data, 0, count));
                    sync.Set();//we can exit the main thread now
                };
            });
        }

        [Test]
        public void CanWriteBytes()
        {
            var testData = new byte[] { 0, 1, 2, 3, 4 };
            ServerHelper.EchoServer((server, port, sync) =>
            {
                var socket = new TcpSocket(TcpSocket.TcpSocketType.IPv4, false);
                socket.Connect(port, null, self => self.Write(testData));
                socket.Data += (data, count) =>
                {
                    Assert.AreEqual(5, count, "Length not met");
                    for (var i = 0; i < count; i++)
                    {
                        Assert.AreEqual(testData[i], data[i], string.Format("Checking data[{0}]", i));
                    }

                    sync.Set();//we can exit the main thread now
                };
            });
        }

        [Test]
        public void CanWriteBytesOfArray()
        {
            var testData = new byte[] { 0, 1, 2, 3, 4 };
            const int index = 1;
            const int length = 2;

            ServerHelper.EchoServer((server, port, sync) =>
            {
                var socket = new TcpSocket(TcpSocket.TcpSocketType.IPv4, false);
                socket.Connect(port, null, self => self.Write(testData, index, length));
                socket.Data += (data, count) =>
                {
                    Assert.AreEqual(length, count, "Length not met");
                    for (var i = 0; i < count; i++)
                    {
                        Assert.AreEqual(testData[i + index], data[i], string.Format("Checking data[{0}]", i));
                    }

                    sync.Set();//we can exit the main thread now
                };
            });
        }
    }
}
