using System;

namespace Ogsn.Network.Internal
{
    public class ClientEventArgs
    {
        public ClientEventType EventType { get; } = ClientEventType.Undefined;
        public Exception Exception { get; } = null;

        public ClientEventArgs(ClientEventType type, Exception exception)
        {
            EventType = type;
            Exception = exception;
        }

        public static ClientEventArgs Info(ClientEventType type)
        {
            return new ClientEventArgs(type, null);
        }

        public static ClientEventArgs SendError(Exception exception)
        {
            return new ClientEventArgs(ClientEventType.SendError, exception);
        }

        public static ClientEventArgs ResponseError(Exception exception)
        {
            return new ClientEventArgs(ClientEventType.ResponseError, exception);
        }
    }
}
