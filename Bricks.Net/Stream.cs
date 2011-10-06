﻿using System;
using System.Text;

namespace Bricks.Net
{
    public abstract class Stream
    {
        #region Readable stuff
        public event Action<byte[], int> Data;
        protected virtual void OnData(byte[] chunk, int count)
        {
            this.Data.TryInvoke(chunk, count);
        }

        public event Action Ended;
        protected virtual void OnEnded()
        {
            this.Ended.TryInvoke();
        }

        public event Action<int> Error;
        protected virtual void OnError(int error)
        {
            this.Error.TryInvoke(error);
        }

        public event Action Close;
        protected virtual void OnClose()
        {
            this.Close.TryInvoke();
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
            this.Drain.TryInvoke();
        }

        public bool Writable { get; set; }

        public abstract bool Write(byte[] chunk);

        public virtual bool Write(string chunk, Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;

            return this.Write(encoding.GetBytes(chunk));
        }

        public virtual void End()
        {
            this.OnEnded();
        }
        
        public virtual void End(byte[] chunk)
        {
            this.Write(chunk);
            this.End();
        }

        public virtual void End(string chunk, Encoding encoding = null)
        {
            this.Write(chunk, encoding);
            this.End();
        }

        #endregion
    }
}
