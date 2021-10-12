namespace Romi.Standard.Sockets.Net
{
    public class SocketEvent
    {
        public SocketEvent(SocketPrincipal socketPrincipal, SocketEventType reservedEvent)
        {
            SocketPrincipal = socketPrincipal;
            ReservedEvent = reservedEvent;
        }

        public SocketPrincipal SocketPrincipal { get; }

        public SocketEventType ReservedEvent { get; }
    }
}
