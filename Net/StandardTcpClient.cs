using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

namespace Romi.Standard.Sockets.Net
{
    public abstract class StandardTcpClient : Client
    {
        private const int HeaderLength = 4;

        private StandardTcpClientState _state;
        private int _contentLength;

        protected StandardTcpClient(Socket socket, SocketThread socketThread)
            : base(socket, socketThread)
        {
            _state = StandardTcpClientState.Header;
        }

        public void SendRawPacket(byte[] rawPacket)
        {
            AddWritePacket(new ArraySegment<byte>(rawPacket));
            Reserve(SocketEventType.Write);
        }

        protected sealed override bool ReadBuffer(SocketBuffer buffer)
        {
            switch (_state)
            {
                case StandardTcpClientState.Header:
                {
                    if (!buffer.TryPoll(HeaderLength, out var bytes))
                        return false;
                    var header = BitConverter.ToUInt32(bytes, 0);
                    if (!IsValidHeader(header))
                        throw new InvalidDataException($"Invalid header.");
                    _contentLength = GetContentLength(header);
                    _state = StandardTcpClientState.Content;
                    buffer.EnsureSize(_contentLength);
                    break;
                }
                case StandardTcpClientState.Content:
                {
                    try
                    {
                        if (!buffer.TryPoll(_contentLength, out var bytes))
                            return false;
                        OnPacket(bytes);
                        _state = StandardTcpClientState.Header;
                        buffer.EnsureSize(HeaderLength);
                    }
                    catch (Exception ex)
                    {
                        Close(ex);
                    }
                    break;
                }
                default:
                    throw new NotImplementedException($"Unknown state {_state}");
            }
            return true;
        }

        protected virtual void OnPacket(byte[] data)
        {
        }

        protected virtual bool IsValidHeader(uint header)
        {
            return true;
        }

        protected virtual int GetContentLength(uint header)
        {
            return Convert.ToInt32(header);
        }
    }
}
