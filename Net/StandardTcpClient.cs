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

        protected sealed override void InitBuffer(SocketBuffer buffer)
        {
            buffer.InitBuffer(_state switch
            {
                StandardTcpClientState.Header => HeaderLength,
                StandardTcpClientState.Content => _contentLength,
                _ => throw new NotImplementedException($"Unknown state {_state}")
            });
        }

        protected sealed override void ReadBuffer(SocketBuffer buffer)
        {
            switch (_state)
            {
                case StandardTcpClientState.Header:
                {
                    var header = Convert.ToUInt32(buffer.Poll(HeaderLength));
                    if (!IsValidHeader(header))
                        throw new InvalidDataException($"Invalid header.");
                    _contentLength = GetContentLength(header);
                    _state = StandardTcpClientState.Content;
                    break;
                }
                case StandardTcpClientState.Content:
                {
                    try
                    {
                        OnPacket(buffer.Poll(buffer.Size));
                        _state = StandardTcpClientState.Header;
                    }
                    catch (Exception ex)
                    {
                        Close($"OnPacketError {ex.Message}");
                    }
                    break;
                }
                default:
                    throw new NotImplementedException($"Unknown state {_state}");
            }
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
