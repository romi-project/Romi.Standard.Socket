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
        private IPEndPoint _endPoint;

        protected Connector(AddressFamily addressFamily, SocketThread socketThread)
            : base(new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp), socketThread)
        {
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
                return IsClosed ? null : Socket;
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
        }

        public override void OnClose()
        {
        }

        internal override void Connected()
        {
            try
            {
                ConnectClient(Socket);
                _connectWaitHandle.Set();
            }
            catch (Exception ex)
            {
                Close(ex, 1);
            }
        }

        internal override void Closed()
        {
            try
            {
                Socket.Close();
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
                Socket.BeginConnect(_endPoint, EndConnect, null);
            }
            catch (ObjectDisposedException)
            {
                Close(CloseReasonInfo.Empty);
            }
            catch (Exception ex)
            {
                Close(ex);
            }
        }

        private void EndConnect(IAsyncResult ar)
        {
            try
            {
                Socket.EndConnect(ar);
                Reserve(SocketEventType.Connect);
            }
            catch (ObjectDisposedException)
            {
                Close(CloseReasonInfo.Empty);
            }
            catch (Exception ex)
            {
                Close(ex);
            }
        }
    }
}
