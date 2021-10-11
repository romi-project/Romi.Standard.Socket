namespace Romi.Standard.Sockets
{
    public class RMClientBase
    {
        private readonly Protocol protocol;
        private readonly Socket socket;
        
        public RMClientBase(Protocol protocol)
        {
            this.protocol = protocol;
        }
        
        
    }
}