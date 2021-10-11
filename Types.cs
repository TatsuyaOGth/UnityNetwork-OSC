using System;

namespace Ogsn.Network
{
    public enum Protocol
    {
        UDP,
        TCP,
    }

    public enum ClientEventType
    {
        Undefined,
        ConnectionRequested,
        Connecting,
        Connected,
        DisconnectionRequested,
        Disconnecting,
        Disconnected,
        Sended,
        ResponseReceived,
        ConnectionError,
        SendError,
        ResponseError,
    }

    public enum ServerEventType
    {
        Undefined,
        Opening,
        Opened,
        Closing,
        Closed,
        ReceiveThreadStarted,
        ReceiveThreadStopped,
        WaitingForConnection,
        Connected,
        Disconnected,
        DataReceived,
        ResponseSended,
        ReceiveError,
        ReceiveHandleError,
    }

    public enum InitCallbackType
    {
        None,
        Awake,
        Start,
        OnEnable,
    }

    public enum UpdateCallbackType
    {
        None,
        Update,
        FixedUpdate,
        LateUpdate,
        Async,
    }

    [Flags]
    public enum LogLevels
    {
        None = 0,
        Verbose = 1,
        Notice = 2,
        Worning = 4,
        Error = 8,
    }
}
