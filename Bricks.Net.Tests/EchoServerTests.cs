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
                    Assert.AreEqual(5, count, "hello.Length");
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
            byte[] testData = GetTestData("Bricks.Net.Tests.Resources.Boston City Flow.jpg");
            int expectedLength = testData.Length;
            int index = 0;
            var bufferSize = 8192;
            var packetCount = (expectedLength / bufferSize) + (expectedLength % bufferSize == 0 ? 0 : 1);

            ServerHelper.EchoServer((server, port, sync) =>
            {
                var socket = new TcpSocket(TcpSocketType.IPv4);
                socket.Connect(port, connectedCallback: self => self.Write(testData, 0, expectedLength));
                socket.Data += (data, count) =>
                {
                    for (var i = 0; (i < count && i + index < expectedLength); i++)
                    {
                        Assert.AreEqual(testData[i + index], data[i],
                                        string.Format("Checking testData[{0}]", i + index));
                    }

                    index += count;
                    
                    Assert.IsTrue(index <= expectedLength, "index is too big");
                    
                    if(index == expectedLength)
                        sync.SignalAndWait();
                };
            }, timeout: 30);

            Assert.AreEqual(expectedLength, index, "Read in expected length");
        }

        [Test]
        public void CanWriteDuringADataEvent()
        {
            ServerHelper.EchoServer((server, port, sync) =>
            {
                var socket = new TcpSocket(TcpSocketType.IPv4);
                var lastThingWritten = "test";
                socket.Connect(port, connectedCallback: self => self.Write(lastThingWritten));
                socket.Data += (data, count) =>
                {
                    Assert.AreEqual(lastThingWritten.Length, count, "Length");
                    Assert.AreEqual(lastThingWritten, Encoding.UTF8.GetString(data, 0, count));

                    lastThingWritten += "test2";
                    socket.Write(lastThingWritten);

                    if(sync.ParticipantsRemaining > 1)
                        sync.RemoveParticipant();
                    else
                        sync.SignalAndWait();//we can exit the main thread now
                };
            }, 6);
        }

        private static byte[] GetTestData(string name)
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
            Assert.NotNull(stream, "image not found");
            var testData = new byte[stream.Length];
            Assert.AreEqual(testData.Length, stream.Read(testData, 0, testData.Length), "Didn't read full image");
            return testData;
        }
    }
}
