using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bricks.Http
{
    public class ServerResponse
    {
        public int StatusCode { get; private set; }
        private readonly Dictionary<string, string> _headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public void WriteContinue()
        {
            throw new NotImplementedException();
        }

        public void WriteHead(int statusCode, string reasonPhrase = null, Dictionary<string, string> headers = null)
        {
            throw new NotImplementedException();
        }

        public void SetHeader(string name, string value)
        {
            if (this._headers.ContainsKey(name))
                this._headers[name] = value;
            else
                this._headers.Add(name, value);
        }

        public string GetHeader(string name)
        {
            if (this._headers.ContainsKey(name))
                return this._headers[name];
            return null;
        }

        public void RemoveHeader(string name)
        {
            this._headers.Remove(name);
        }

        public void Write(byte[] chunk, Encoding encoding)
        {
            throw new NotImplementedException();
        }

        public void Write(string chunk)
        {
            throw new NotImplementedException();
        }

        public void AddTrailers(Dictionary<string, string> headers)
        {
            throw new NotImplementedException();
        }

        public void End(byte[] chunk, Encoding encoding)
        {
            this.Write(chunk, encoding);
            this.End();
        }

        public void End(string chunk)
        {
            this.Write(chunk);
            this.End();
        }

        public void End()
        {
            throw new NotImplementedException();
        }
    }
}
