using System;

namespace Ogsn.Network
{
    public class ClientEventArgs
    {
        public ClientEventType EventType { get; } = ClientEventType.Undefined;
        public byte[] Data { get; }
        public Exception Exception { get; } = null;

        public ClientEventArgs(ClientEventType type, byte[] data, Exception exception)
        {
            EventType = type;
            Data = data;
            Exception = exception;
        }

        public static ClientEventArgs Info(ClientEventType type)
        {
            return new ClientEventArgs(type, null, null);
        }

        public static ClientEventArgs DataSended(byte[] data)
        {
            return new ClientEventArgs(ClientEventType.Sended, data, null);
        }

        public static ClientEventArgs ResponseReceived(byte[] data)
        {
            return new ClientEventArgs(ClientEventType.ResponseReceived, data, null);
        }

        public static ClientEventArgs ConnectionError(Exception exception)
        {
            return new ClientEventArgs(ClientEventType.ConnectionError, null, exception);
        }

        public static ClientEventArgs SendError(byte[] data, Exception exception)
        {
            return new ClientEventArgs(ClientEventType.SendError, data, exception);
        }

        public static ClientEventArgs ResponseError(Exception exception)
        {
            return new ClientEventArgs(ClientEventType.ResponseError, null, exception);
        }
    }
}
