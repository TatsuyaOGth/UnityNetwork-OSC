using System;
using UnityEngine.Events;

namespace Ogsn.Network
{
    [Serializable]
    public class ClientEventHandler : UnityEvent<ClientEventType> { }

    [Serializable]
    public class ServerEventHandler : UnityEvent<ServerEventType> { }

    [Serializable]
    public class OscMessageReceivedArgs : UnityEvent<OscReceiver, OscMessage> { }
}
