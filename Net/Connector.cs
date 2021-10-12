using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Romi.Standard.Sockets.Net
{
    public abstract class Connector : SocketPrincipal
    {
        private readonly ManualResetEventSlim _connectWaitHandle = new();
        private readonly Socket _socket;
        private readonly SocketThread _socketThread;
        private IPEndPoint _endPoint;

        protected Connector(Socket socket, SocketThread socketThread)
            : base(socket, socketThread)
        {
            _socket = socket;
            _socketThread = socketThread;
        }

        ~Connector()
        {
            _connectWaitHandle.Dispose();
        }

        public Task<Socket> ConnectRawAsync(IPEndPoint endPoint)
        {
            _endPoint = endPoint;
            BeginConnect();
            return Task.Run(() =>
            {
                _connectWaitHandle.Wait();
                return IsClosed ? null : _socket;
            });
        }

        public Socket ConnectRaw(IPEndPoint endPoint)
        {
            var task = ConnectRawAsync(endPoint);
            task.Wait();
            return task.Result;
        }

        public override void OnConnect()
        {
            try
            {
                ConnectClient(_socket);
                _connectWaitHandle.Set();
            }
            catch (Exception ex)
            {
                Close($"Exception {ex.Message}", 2);
            }
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
            _connectWaitHandle.Set();
        }

        protected abstract void ConnectClient(Socket socket);

        private void BeginConnect()
        {
            try
            {
                _socket.BeginConnect(_endPoint, EndConnect, null);
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

        private void EndConnect(IAsyncResult ar)
        {
            try
            {
                _socket.EndConnect(ar);
                Reserve(new SocketEvent(this, SocketEventType.Connect));
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
