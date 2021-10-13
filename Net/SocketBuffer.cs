using System;
using System.Linq;

namespace Romi.Standard.Sockets.Net
{
    public class SocketBuffer
    {
        private const int DefaultBufferSize = 256 * 1024;
        public byte[] Buffer;
        public int Offset;

        public SocketBuffer()
        {
            Offset = 0;
            EnsureSize(DefaultBufferSize);
        }

        public void EnsureSize(int expected)
        {
            if (Buffer == null)
                Buffer = new byte[expected];
            else if (Buffer.Length < expected)
                Array.Resize(ref Buffer, expected);
        }

        public bool TryPoll(int size, out byte[] ret)
        {
            if (Offset < size) // lack
            {
                ret = null;
                return false;
            }
            ret = Buffer.Take(size).ToArray();
            if (Offset > size)
                Array.Copy(Buffer, size, Buffer, 0, Offset - size);
            Offset -= size;
            return true;
        }
    }
}
