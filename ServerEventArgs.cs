using System;

namespace Ogsn.Network
{
    public class ServerEventArgs
    {
        public ServerEventType EventType { get; } = ServerEventType.Undefined;
        public byte[] Data { get; }
        public Exception Exception { get; } = null;

        public ServerEventArgs(ServerEventType type, byte[] data, Exception exception)
        {
            EventType = type;
            Data = data;
            Exception = exception;
        }

        public static ServerEventArgs Info(ServerEventType type)
        {
            return new ServerEventArgs(type, null, null);
        }

        public static ServerEventArgs DataReceived(byte[] data)
        {
            return new ServerEventArgs(ServerEventType.DataReceived, data, null);
        }

        public static ServerEventArgs ResponseSended(byte[] data)
        {
            return new ServerEventArgs(ServerEventType.ResponseSended, data, null);
        }

        public static ServerEventArgs Disconnected(Exception exception)
        {
            return new ServerEventArgs(ServerEventType.Disconnected, null, exception);
        }

        public static ServerEventArgs ReceiveError(Exception exception)
        {
            return new ServerEventArgs(ServerEventType.ReceiveError, null, exception);
        }

        public static ServerEventArgs ReceiveHandleError(Exception exception)
        {
            return new ServerEventArgs(ServerEventType.ReceiveHandleError, null, exception);
        }
    }
}
