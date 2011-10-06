using System;
using System.Collections.Generic;
using System.Linq;
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
    }
}
