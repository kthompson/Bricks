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
            new EchoServerTests().CanReadAndWrite300Kb();
        }
    }
}
