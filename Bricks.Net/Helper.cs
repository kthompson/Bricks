using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bricks.Net
{
    public static class Helper
    {
        public static void TryInvoke(this Action callback)
        {
            if (callback != null)
                callback();
        }

        public static void TryInvoke<T>(this Action<T> callback, T obj)
        {
            if (callback != null)
                callback(obj);
        }

        public static void TryInvoke<T1, T2>(this Action<T1, T2> callback, T1 obj1, T2 obj2)
        {
            if (callback != null)
                callback(obj1, obj2);
        }


        public static void TryInvoke<T1, T2, T3>(this Action<T1, T2, T3> callback, T1 obj1, T2 obj2, T3 obj3)
        {
            if (callback != null)
                callback(obj1, obj2, obj3);
        }


        public static TResult Try<T, TResult>(this T o, Func<TResult> func)
            where T : class
            where TResult : class
        {
            return o == null ? null : func();
        }

        public static TResult Try<T, TResult>(this T o, Func<T, TResult> func)
            where T : class
            where TResult : class
        {
            return o == null ? null : func(o);
        }

        public static Task StartLongRunningTask(Action task, CancellationToken token)
        {
            return Task.Factory.StartNew(task, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public static IPEndPoint ToIPEndPoint(this DnsEndPoint ep, AddressFamily family = AddressFamily.InterNetwork)
        {
            var addresses = Dns.GetHostAddresses(ep.Host);
            var addr = addresses.FirstOrDefault(ip => ip.AddressFamily == family);

            if (addr == null)
            {
                throw new ArgumentException("Unable to retrieve address from specified host name.", "hostName");
            }

            return new IPEndPoint(addr, ep.Port); // Port gets validated here.
        }
    }
}
