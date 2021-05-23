
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
        SendError,
        ResponseError,
        PingSuccessed,
        PingFailed,
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
        DataReceived,
        ResponseSended,
        ReceiveError,
        ReceiveHandleError,
    }


    public enum AutoRunType
    {
        None,
        Awake,
        Start,
        OnEnable,
    }

    public enum UpdateType
    {
        None,
        Update,
        FixedUpdate,
        Async,
    }
}
