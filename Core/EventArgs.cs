using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bricks.Net
{
    public class EventArgs<T> : EventArgs
    {
        public T Data { get; private set; }

        public EventArgs(T data)
        {
            this.Data = data;
        }
    }
}
