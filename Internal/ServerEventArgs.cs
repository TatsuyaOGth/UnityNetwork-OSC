using System;

namespace Ogsn.Network.Internal
{
    public class ServerEventArgs
    {
        public ServerEventType EventType { get; } = ServerEventType.Undefined;
        public Exception Exception { get; } = null;

        public ServerEventArgs(ServerEventType type, Exception exception)
        {
            EventType = type;
            Exception = exception;
        }

        public static ServerEventArgs Info(ServerEventType type)
        {
            return new ServerEventArgs(type, null);
        }

        public static ServerEventArgs ReceiveError(Exception exception)
        {
            return new ServerEventArgs(ServerEventType.ReceiveError, exception);
        }

        public static ServerEventArgs ReceiveHandleError(Exception exception)
        {
            return new ServerEventArgs(ServerEventType.ReceiveHandleError, exception);
        }
    }
}
