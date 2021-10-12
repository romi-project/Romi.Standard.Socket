using System;
using System.Net.Sockets;

namespace Romi.Standard.Sockets.Net
{
    public abstract class Connector : SocketPrincipal
    {
        private readonly Socket _socket;
        private readonly SocketThread _socketThread;

        protected Connector(Socket socket, SocketThread socketThread)
            : base(socket, socketThread)
        {
            _socket = socket;
            _socketThread = socketThread;
        }

        public override void OnConnect()
        {
        }

        public override void OnClose()
        {
        }
    }
}
