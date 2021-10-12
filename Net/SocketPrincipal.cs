using System;
using System.Diagnostics;
using System.Net.Sockets;

namespace Romi.Standard.Sockets.Net
{
    public abstract class SocketPrincipal
    {
        protected readonly Socket Socket;
        private readonly SocketThread _socketThread;
        private readonly object _syncRoot = new();
        private bool _closed;
        private string _closeReason;

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

        public string CloseReason
        {
            get
            {
                lock (_syncRoot)
                    return _closeReason;
            }
        }

        public void Close(string closeReason = "Calling Close", int callingStackFrame = 1)
        {
            lock (_syncRoot)
            {
                if (_closed)
                    return;
                _closed = true;
                if (closeReason != null)
                    _closeReason = $"{closeReason} at {new StackTrace().GetFrame(callingStackFrame)}";
                Reserve(SocketEventType.Close);
            }
        }

        public virtual void OnConnect() => throw new NotSupportedException();

        public virtual void OnAccept() => throw new NotSupportedException();

        public virtual void OnRead() => throw new NotSupportedException();

        public virtual void OnWrite() => throw new NotSupportedException();

        public virtual void OnClose() => throw new NotSupportedException();

        public virtual void OnOutOfBand() => throw new NotSupportedException();

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
