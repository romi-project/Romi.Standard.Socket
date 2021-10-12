using System;
using System.Collections.Generic;
using System.Threading;
using Romi.Standard.Sockets.Threading;

namespace Romi.Standard.Sockets.Net
{
    public class SocketThread : EventThread
    {
        private readonly ManualResetEventSlim _eventHandle = new();
        private readonly ManualResetEventSlim _stopWaitHandle = new();
        private readonly object _syncRoot = new();
        private readonly Queue<SocketEvent> _events = new();
        private bool _stopped;

        ~SocketThread()
        {
            _eventHandle.Dispose();
            _stopWaitHandle.Dispose();
        }

        protected override void Run()
        {
            while (true)
            {
                _eventHandle.Wait();
                while (TryDequeue(out var socketEvent))
                    Execute(socketEvent);
                if (!ReadyNextEvent())
                    break;
            }
            _stopWaitHandle.Set();
        }

        public void Reserve(SocketEvent socketEvent)
        {
            lock (_syncRoot)
            {
                _events.Enqueue(socketEvent);
                _eventHandle.Set();
            }
        }

        public void Stop()
        {
            lock (_syncRoot)
            {
                _stopped = true;
                _eventHandle.Set();
            }
        }

        public void WaitForEnd()
        {
            _stopWaitHandle.Wait();
        }

        private bool TryDequeue(out SocketEvent socketEvent)
        {
            lock (_syncRoot)
            {
                if (_events.Count == 0)
                {
                    socketEvent = null;
                    return false;
                }
                socketEvent = _events.Dequeue();
                return true;
            }
        }

        private static void Execute(SocketEvent socketEvent)
        {
            var socketPrincipal = socketEvent.SocketPrincipal;
            var reservedEvent = socketEvent.ReservedEvent;
            if (reservedEvent.HasFlag(SocketEventType.Close))
                socketPrincipal.OnClose();
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
        }

        private bool ReadyNextEvent()
        {
            lock (_syncRoot)
            {
                if (_stopped)
                    return false;
                _eventHandle.Reset();
                return true;
            }
        }
    }
}
