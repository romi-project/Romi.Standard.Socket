using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Romi.Standard.Sockets.Net
{
    public abstract class Listener : SocketPrincipal
    {
        private readonly SocketThread _socketThread;
        private readonly Socket _socket;
        private IPEndPoint _endPoint;

        protected Listener(Socket socket, SocketThread socketThread)
            : base (socket, socketThread)
        {
            _socket = socket;
            _socketThread = socketThread;
        }

        public void Bind(IPEndPoint endPoint)
        {
            _socket.Bind(_endPoint = endPoint);
            _socket.Listen(100);
        }

        private void BeginAccept()
        {
            try
            {
                _socket.BeginAccept(EndAccept, null);
            }
            catch (ObjectDisposedException)
            {
                Close(null);
            }
            catch (Exception ex)
            {
                Close($"Exception {ex.Message}", 1);
            }
        }

        private void EndAccept(IAsyncResult ar)
        {
            try
            {
                var socket = _socket.EndAccept(ar);
            }
            catch (ObjectDisposedException)
            {
                Close(null);
            }
            catch (Exception ex)
            {
                Close($"Exception {ex.Message}", 1);
            }
        }
    }
}
