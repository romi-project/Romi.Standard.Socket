using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace Romi.Standard.Sockets.Net
{
    public abstract class Listener : SocketPrincipal
    {
        private readonly ConcurrentQueue<Socket> _acceptedSockets = new();
        private IPEndPoint _endPoint;

        protected Listener(AddressFamily addressFamily, SocketThread socketThread)
            : base (new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp), socketThread)
        {
        }

        public void Bind(IPEndPoint endPoint)
        {
            Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            Socket.Bind(_endPoint = endPoint);
        }

        public void Listen()
        {
            Socket.Listen(100);
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
                Socket.Close();
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
                Socket.BeginAccept(EndAccept, null);
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
                _acceptedSockets.Enqueue(Socket.EndAccept(ar));
                Reserve(SocketEventType.Accept);
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
