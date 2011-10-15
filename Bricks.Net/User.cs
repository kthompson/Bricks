using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bricks.Net
{
    public class User : IDisposable
    {
        private readonly Action _disposeAction;

        public User(Action disposeAction)
        {
            _disposeAction = disposeAction;
        }

        public void Dispose()
        {
            
            _disposeAction();
        }
    }
}
