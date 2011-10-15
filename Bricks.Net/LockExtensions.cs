using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Bricks.Net
{
    public static class LockExtensions
    {
        public static IDisposable ReadLock(this ReaderWriterLockSlim rwl)
        {
            rwl.EnterReadLock();
            return new User(rwl.ExitReadLock);
        }

        public static IDisposable WriteLock(this ReaderWriterLockSlim rwl)
        {
            rwl.EnterWriteLock();
            return new User(rwl.ExitWriteLock);
        }
    }
}
