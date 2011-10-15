using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Bricks.Net;
using Bricks.Net.Tests;
using NUnit.Framework;

namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var testData = new byte[] { 0, 1, 2, 3, 4 };
            ServerHelper.EchoServer((server, port, sync) =>
            {
                var socket = new TcpSocket(TcpSocketType.IPv4);
                socket.Connect(port, connectedCallback: self => self.Write(testData));
                socket.Data += (data, count) =>
                {
                    Assert.AreEqual(5, count, "Length not met");
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
