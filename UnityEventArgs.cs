using System;
using UnityEngine.Events;

namespace Ogsn.Network
{
    [Serializable]
    public class ClientEventHandler : UnityEvent<ClientEventArgs> { }

    [Serializable]
    public class ServerEventHandler : UnityEvent<ServerEventArgs> { }


    [Serializable]
    public class DataReceivedEventHandler : UnityEvent<byte[]> { }

    [Serializable]
    public class OscMessageReceivedEventHandler : UnityEvent<OSC.OscMessage> { }
}
