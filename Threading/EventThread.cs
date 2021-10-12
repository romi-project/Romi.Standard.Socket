using System.Threading;

namespace Romi.Standard.Sockets.Threading
{
    public abstract class EventThread
    {
        protected abstract void Run();

        public static implicit operator ThreadStart(EventThread eventThread)
        {
            return new ThreadStart(eventThread.Run);
        }
    }
}
