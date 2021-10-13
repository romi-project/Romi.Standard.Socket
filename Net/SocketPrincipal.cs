using System;
using System.Net.Sockets;

namespace Romi.Standard.Sockets.Net
{
    public abstract class SocketPrincipal
    {
        protected readonly Socket Socket;
        private readonly SocketThread _socketThread;
        private readonly object _syncRoot = new();
        private bool _closed;
        private CloseReasonInfo _closeReason;

        protected SocketPrincipal(Socket socket, SocketThread socketThread)
        {
            Socket = socket;
            _socketThread = socketThread;
        }

        public bool IsClosed
        {
            get
            {
                lock (_syncRoot)
                    return _closed;
            }
        }

        public CloseReasonInfo CloseReason
        {
            get
            {
                lock (_syncRoot)
                    return _closeReason;
            }
        }

        public void Close(Exception ex, int callingStackFrame = 0)
            => Close(new CloseReasonInfo(ex, callingStackFrame));

        public void Close(CloseReasonInfo closeReason = null)
        {
            lock (_syncRoot)
            {
                if (_closed)
                    return;
                _closed = true;
                _closeReason = closeReason ?? CloseReasonInfo.Default;
                Reserve(SocketEventType.Close);
            }
        }

        public virtual void OnConnect() => throw new NotSupportedException();

        public virtual void OnAccept() => throw new NotSupportedException();

        public virtual void OnRead() => throw new NotSupportedException();

        public virtual void OnWrite() => throw new NotSupportedException();

        public virtual void OnClose() => throw new NotSupportedException();

        public virtual void OnOutOfBand() => throw new NotSupportedException();

        internal virtual void Connected() {}
        internal virtual void Accepted() {}
        internal virtual void Read() {}
        internal virtual void Written() {}
        internal virtual void Closed() {}
        internal virtual void OutOfBand() {}

        public void Reserve(SocketEventType eventType)
        {
            _socketThread.Reserve(new SocketEvent(this, eventType));
        }

        internal void Reserve(SocketEvent socketEvent)
        {
            _socketThread.Reserve(socketEvent);
        }
    }
}
