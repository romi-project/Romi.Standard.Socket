using System;

namespace Romi.Standard.Sockets.Net
{
    [Flags]
    public enum SocketEventType
    {
        Accept = 1 << 0,
        Connect = 1 << 1,
        Read = 1 << 2,
        Write = 1 << 3,
        Close = 1 << 4,
        OutOfBand = 1 << 5
    }
}
