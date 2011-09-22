using System;
using System.Text;

namespace Bricks.Net
{
    public abstract class Stream
    {
        #region Readable stuff
        public event Action<byte[]> Data;
        protected virtual void OnData(byte[] chunk)
        {
            var handler = this.Data;
            if (handler != null)
                handler(chunk);
        }

        public event Action Ended;
        protected virtual void OnEnded()
        {
            var handler = this.Ended;
            if (handler != null)
                handler();
        }

        public event Action<int> Error;
        protected virtual void OnError(int error)
        {
            var handler = this.Error;
            if (handler != null)
                handler(error);
        }

        public event Action Close;
        protected virtual void OnClose()
        {
            var handler = this.Close;
            if (handler != null)
                handler();
        }


        public bool Readable { get; set; }

        public abstract void Pause();
        public abstract void Resume();
        public abstract void Destroy();
        public abstract void DestroySoon();

        #endregion

        #region Writeable stuff
        public event Action Drain;
        protected virtual void OnDrain()
        {
            var handler = this.Drain;
            if (handler != null)
                handler();
        }

        public bool Writable { get; set; }

        public abstract bool Write(byte[] chunk);
        public abstract bool Write(string chunk, Encoding encoding = null);
        public abstract void End();
        public abstract void End(byte[] chunk);
        public abstract void End(string chunk, Encoding encoding = null);

        #endregion
    }
}
