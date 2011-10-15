using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using NUnit.Framework;

namespace Bricks.Net.Tests
{
    [TestFixture]
    public class EchoServerTests
    {
        [Test]
        public void CanWriteString()
        {
            ServerHelper.EchoServer((server, port, sync) =>
            {
                var socket = new TcpSocket(TcpSocketType.IPv4);
                socket.Connect(port, connectedCallback: self => self.Write("hello"));
                socket.Data += (data, count) =>
                {
                    Assert.AreEqual("hello", Encoding.UTF8.GetString(data, 0, count));
                    sync.SignalAndWait();//we can exit the main thread now
                };
            });
        }

        [Test]
        public void CanWriteBytes()
        {
            var testData = new byte[] { 0, 1, 2, 3, 4 };
            ServerHelper.EchoServer((server, port, sync) =>
            {
                var socket = new TcpSocket(TcpSocketType.IPv4);

                socket.Data += (data, count) =>
                {
                    Assert.AreEqual(5, count, "Length not met");
                    for (var i = 0; i < count; i++)
                    {
                        Assert.AreEqual(testData[i], data[i], string.Format("Checking data[{0}]", i));
                    }

                    sync.SignalAndWait();//we can exit the main thread now
                };

                socket.Connect(port, connectedCallback: self => self.Write(testData));
            }, 2, 60);
        }

        [Test]
        public void CanWriteBytesOfArray()
        {
            var testData = new byte[] { 0, 1, 2, 3, 4 };
            const int index = 1;
            const int length = 2;

            ServerHelper.EchoServer((server, port, sync) =>
            {
                var socket = new TcpSocket(TcpSocketType.IPv4);
                socket.Connect(port, connectedCallback: self => self.Write(testData, index, length));
                socket.Data += (data, count) =>
                {
                    Assert.AreEqual(length, count, "Length not met");
                    for (var i = 0; i < count; i++)
                    {
                        Assert.AreEqual(testData[i + index], data[i], string.Format("Checking data[{0}]", i));
                    }

                    sync.SignalAndWait();//we can exit the main thread now
                };
            });
        }

        [Test]
        public void CanReadAndWrite300Kb()
        {
            var t = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Bricks.Net.Tests.Resources.Boston City Flow.jpg");
            Assert.NotNull(stream, "image not found");
            var testData = new byte[stream.Length];
            Assert.AreEqual(testData.Length, stream.Read(testData, 0, testData.Length), "Didn't read full image");

            ServerHelper.EchoServer((server, port, sync) =>
            {
                var socket = new TcpSocket(TcpSocketType.IPv4);
                socket.Connect(port, connectedCallback: self => self.Write(testData, 0, testData.Length));
                socket.Data += (data, count) =>
                {
                    Assert.AreEqual(testData.Length, count, "Length not met");
                    for (var i = 0; i < count; i++)
                    {
                        Assert.AreEqual(testData[i], data[i], string.Format("Checking data[{0}]", i));
                    }

                    sync.SignalAndWait();//we can exit the main thread now
                };
            });
        }
    }
}
