using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

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

        public static IPEndPoint EndPointFromHostname(string host, int port)
        {
            if (string.IsNullOrEmpty(host))
                return new IPEndPoint(IPAddress.Any, port);

            var ip = IPFromHostname(host);

            return ip.Try(() => new IPEndPoint(ip, port));
        }

        public static IPAddress IPFromHostname(string host)
        {
            IPAddress ip;
            if (IPAddress.TryParse(host, out ip))
                return ip;

            return Dns.GetHostAddresses(host).FirstOrDefault();
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
    }
}
