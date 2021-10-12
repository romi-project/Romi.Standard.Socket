using System;
using System.Linq;

namespace Romi.Standard.Sockets.Net
{
    public class SocketBuffer
    {
        public byte[] Buffer;
        public int Size;
        public int Read;
        public int Remaining => Size - Read;

        public void InitBuffer(int expected)
        {
            Read = 0;
            Size = expected;
            if (Buffer == null)
                Buffer = new byte[expected];
            else if (Buffer.Length < expected)
                Array.Resize(ref Buffer, expected);
        }

        public byte[] Poll(int size)
        {
            size = Math.Min(size, Size);
            var ret = Buffer.Take(size).ToArray();
            var remain = Size - size;
            if (remain > 0)
            {
                Array.Copy(Buffer, size, Buffer, 0, remain);
                Array.Resize(ref Buffer, remain);
            }
            else
                Buffer = null;
            Size -= size;
            return ret;
        }
    }
}
