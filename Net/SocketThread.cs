using System;
using System.Collections.Generic;
using System.Threading;
using Romi.Standard.Sockets.Threading;

namespace Romi.Standard.Sockets.Net
{
    public class SocketThread : EventThread
    {
        private readonly ManualResetEventSlim _eventHandle = new();
        private readonly object _syncRoot = new();
        private readonly Queue<SocketEvent> _events = new();
        private bool _stopped;

        protected override void Run()
        {
            while (!_stopped)
            {
                _eventHandle.Wait();
                while (true)
                {
                    SocketEvent socketEvent;
                    lock (_syncRoot)
                    {
                        if (_events.Count == 0)
                            goto Next;
                        socketEvent = _events.Dequeue();
                    }

                    var socketPrincipal = socketEvent.SocketPrincipal;
                    var reservedEvent = socketEvent.ReservedEvent;
                    if (reservedEvent.HasFlag(SocketEventType.OutOfBand))
                        socketPrincipal.OnOutOfBand();
                    if (reservedEvent.HasFlag(SocketEventType.Accept))
                        socketPrincipal.OnAccept();
                    if (reservedEvent.HasFlag(SocketEventType.Connect))
                        socketPrincipal.OnConnect();
                    if (reservedEvent.HasFlag(SocketEventType.Read))
                        socketPrincipal.OnRead();
                    if (reservedEvent.HasFlag(SocketEventType.Write))
                        socketPrincipal.OnWrite();
                    if (reservedEvent.HasFlag(SocketEventType.Close))
                        socketPrincipal.OnClose();
                }
                Next:
                _eventHandle.Reset();
            }
            _eventHandle.Dispose();
        }

        public void Reserve(SocketEvent socketEvent)
        {
            lock (_syncRoot)
                _events.Enqueue(socketEvent);
            _eventHandle.Set();
        }

        public void Stop()
        {
            _stopped = true;
            _eventHandle.Set();
        }
    }
}
