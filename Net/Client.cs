using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Romi.Standard.Sockets.Net
{
    public abstract class Client : SocketPrincipal
    {
        private readonly List<ArraySegment<byte>> _writePacketList;
        private readonly object _writeSyncRoot = new object();
        private readonly EndPoint _localAddress;
        private readonly EndPoint _remoteAddress;
        private readonly SocketBuffer _socketBuffer;

        protected Client(Socket socket, SocketThread socketThread)
            : base(socket, socketThread)
        {
            _localAddress = Socket.LocalEndPoint;
            _remoteAddress = Socket.RemoteEndPoint;
            _writePacketList = new List<ArraySegment<byte>>();
            _socketBuffer = new SocketBuffer();
        }

        public string LocalAddress => _localAddress.ToString();
        public string RemoteAddress => _remoteAddress.ToString();

        public override void OnConnect()
        {
        }

        public override void OnRead()
        {
        }

        public override void OnWrite()
        {
        }

        public override void OnClose()
        {
        }

        internal override void Connected()
        {
            BeginReceive();
        }

        internal override void Closed()
        {
            try
            {
                Socket.Shutdown(SocketShutdown.Both);
                Socket.Close();
            }
            catch
            {
                // ignored
            }
        }

        internal override void Read()
        {
            try
            {
                while (true)
                {
                    if (!ReadBuffer(_socketBuffer))
                        break;
                }
                BeginReceive();
            }
            catch (Exception ex)
            {
                Close(ex);
            }
        }

        internal override void Written()
        {
            BeginSend();
        }

        protected abstract bool ReadBuffer(SocketBuffer buffer);

        protected void AddWritePackets(IEnumerable<ArraySegment<byte>> writePackets)
        {
            if (IsClosed)
                return;
            lock (_writeSyncRoot)
            {
                _writePacketList.AddRange(writePackets);
            }
        }

        protected void AddWritePacket(ArraySegment<byte> writePacket)
        {
            if (IsClosed)
                return;
            lock (_writeSyncRoot)
            {
                _writePacketList.Add(writePacket);
            }
        }

        private void BeginReceive()
        {
            var offset = _socketBuffer.Offset;
            var size = _socketBuffer.Buffer.Length - _socketBuffer.Offset;
            Socket.BeginReceive(_socketBuffer.Buffer, offset, size, SocketFlags.None, out var error, EndReceive, null);
            if (!IsContinuableSocketError(error))
                Close(new CloseReasonInfo(true, "SocketError", error.ToString()));
        }

        private void EndReceive(IAsyncResult ar)
        {
            try
            {
                var read = Socket.EndReceive(ar, out var error);
                if (read == 0)
                {
                    Close(new CloseReasonInfo(false, "Shutdown gracefully"));
                    return;
                }
                if (!IsContinuableSocketError(error))
                {
                    Close(new CloseReasonInfo(true, "SocketError", error.ToString()));
                    return;
                }
                _socketBuffer.Offset += read;
                Reserve(SocketEventType.Read);
            }
            catch (Exception ex)
            {
                Close(ex);
            }
        }

        private void BeginSend()
        {
            try
            {
                if (!PollWritePacketList(out var writePacketList))
                    return;
                Socket.BeginSend(writePacketList, SocketFlags.None, out var error, EndSend, null);
                if (!IsContinuableSocketError(error))
                    Close(new CloseReasonInfo(true, "SocketError", error.ToString()));
            }
            catch (Exception ex)
            {
                Close(ex);
            }
        }

        private bool PollWritePacketList(out IList<ArraySegment<byte>> writePacketList)
        {
            lock (_writeSyncRoot)
            {
                if (_writePacketList.Count == 0)
                {
                    writePacketList = Enumerable.Empty<ArraySegment<byte>>().ToList();
                    return false;
                }
                writePacketList = new List<ArraySegment<byte>>(_writePacketList);
                _writePacketList.Clear();
            }

            return true;
        }

        private void EndSend(IAsyncResult ar)
        {
            try
            {
                Socket.EndSend(ar, out var error);
                if (!IsContinuableSocketError(error))
                    Close(new CloseReasonInfo(true, "SocketError", error.ToString()));
            }
            catch (Exception ex)
            {
                Close(ex);
            }
        }

        private bool IsContinuableSocketError(SocketError error)
        {
            return error switch
            {
                SocketError.IOPending => true,
                SocketError.Success => true,
                _ => false
            };
        }
    }
}
