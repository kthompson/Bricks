using System;
using System.Net;
using System.Net.Sockets;
using NetSocket = System.Net.Sockets.Socket;

namespace Bricks.Net
{
    public class Server
    {
        public int? BackLog { get; private set; }
        public int? MaxConnections { get; set; }
        public int ConnectionCount { get; internal set; }
        public bool AllowHalfOpen { get; private set; }

        protected TcpServer Handle { get; set; }

        public Server()
        {
            
        }

        public void Listen(int port, IPAddress address)
        {
            
        }

        private static void Listen(Server self, IPAddress address, int port)
        {
            if (self.Handle != null)
            {
                // assign handle in listen, and clean up if bind or listen fails
                self.Handle = new TcpServer();
            }

            self.Handle.Connection += (sender, args) => OnConnection(self, args.Socket);

            var success = self.Handle.Bind(address, port);
            if (!success)
            {
                self.Handle.Close();
                self.Handle = null;

                //TODO: throw error
            }
            else
            {
                success = self.Handle.Listen(self.BackLog ?? 128);
                if (!success)
                {
                    self.Handle.Close();
                    self.Handle = null;

                    //TODO: throw error
                }
                else
                {
                    self.OnListening();
                }
            }
        }

        private static void OnConnection(Server self, Socket clientHandle) 
        {
          if (self.MaxConnections.HasValue && self.ConnectionCount >= self.MaxConnections)
          {
              clientHandle.Close();
              return;
          }

            var socket = new TcpSocket(clientHandle, self.AllowHalfOpen);

          
            socket.Readable = socket.Writable = true;
          socket.Resume();

          self.ConnectionCount++;
          //socket.server = self;

  
          self.OnConnection(socket);
            socket.OnConnect();
        }



        public EventHandler<TcpSocketEventArgs> Connection;
        protected virtual void OnConnection(TcpSocket socket)
        {
            var handler = this.Connection;
            if (handler != null)
                handler(this, new TcpSocketEventArgs(socket));
        }

        public EventHandler Listening;
        protected virtual void OnListening()
        {
            var handler = this.Listening;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

    }
}
