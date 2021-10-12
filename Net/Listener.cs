using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace Romi.Standard.Sockets.Net
{
    public abstract class Listener : SocketPrincipal
    {
        private readonly ConcurrentQueue<Socket> _acceptedSockets = new();
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
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _socket.Bind(_endPoint = endPoint);
        }

        public void Listen()
        {
            _socket.Listen(100);
            BeginAccept();
        }

        public override void OnAccept()
        {
            while (_acceptedSockets.TryDequeue(out var acceptedSocket))
                AcceptClient(acceptedSocket);
        }

        public override void OnClose()
        {
            try
            {
                _socket.Close();
            }
            catch
            {
                // ignored
            }
        }

        protected abstract void AcceptClient(Socket socket);

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
                _acceptedSockets.Enqueue(_socket.EndAccept(ar));
                Reserve(new SocketEvent(this, SocketEventType.Accept));
            }
            catch (ObjectDisposedException)
            {
                Close(null);
                return;
            }
            catch (Exception ex)
            {
                Close($"Exception {ex.Message}", 1);
                return;
            }

            if (IsClosed)
                return;
            BeginAccept();
        }
    }
}
