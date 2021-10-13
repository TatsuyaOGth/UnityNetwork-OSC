using UnityEngine;

namespace Ogsn.Network
{
    using Core;

    public class NetworkServer : MonoBehaviour
    {
        // Presets on Inspector
        [Header("Listen port")]
        public int ListenPort = 50000;

        [Header("Advanced")]
        public Protocol Protocol = Protocol.UDP;
        public InitCallbackType AutoOpen = InitCallbackType.Start;
        public bool CloseOnDisable = true;
        public LogLevels LogLevels = LogLevels.Notice | LogLevels.Worning | LogLevels.Error;

        // Properties
        public IServer Server => _server;
        public bool IsOpened => _server?.IsOpened ?? false;

        // Internal data
        IServer _server;



        public void Open()
        {
            if (_server != null)
                return;

            switch (Protocol)
            {
                case Protocol.UDP:
                    _server = new UDPServer();
                    break;
                case Protocol.TCP:
                    _server = new TCPServer();
                    break;
            }

            _server.NotifyServerEvent += OnServerEventReceived;
            _server.Open(ListenPort);
        }

        public void Close()
        {
            if (_server != null)
            {
                _server.Close();
                _server.NotifyServerEvent -= OnServerEventReceived;
                _server = null;
            }
        }

        public void Awake()
        {
            if (AutoOpen == InitCallbackType.Awake)
            {
                Open();
            }
        }

        public void Start()
        {
            if (AutoOpen == InitCallbackType.Start)
            {
                Open();
            }
        }

        public void OnEnable()
        {
            if (AutoOpen == InitCallbackType.OnEnable)
            {
                Open();
            }
        }

        public void OnDisable()
        {
            if (CloseOnDisable)
            {
                Close();
            }
        }

        public void OnDestroy()
        {
            Close();
        }

        protected virtual void OnServerEventReceived(object sender, ServerEventArgs e)
        {
            if (sender.Equals(_server))
            {
                switch (e.EventType)
                {
                    case ServerEventType.Opening:
                    case ServerEventType.Closing:
                    case ServerEventType.ReceiveThreadStarted:
                    case ServerEventType.ReceiveThreadStopped:
                    case ServerEventType.WaitingForConnection:
                    case ServerEventType.Connected:
                    case ServerEventType.DataReceived:
                    case ServerEventType.ResponseSended:
                        Log($"{e.EventType}: Listen port={ListenPort}({Protocol})", LogType.Log, LogLevels.Verbose);
                        break;

                    case ServerEventType.Opened:
                    case ServerEventType.Closed:
                        Log($"{e.EventType}: Listen port={ListenPort}({Protocol})", LogType.Log, LogLevels.Notice);
                        break;

                    case ServerEventType.Disconnected:
                        Log($"{e.EventType}, {e?.Exception?.Message}: Listen port={ListenPort}({Protocol})", LogType.Warning, LogLevels.Worning);
                        break;

                    case ServerEventType.ReceiveError:
                    case ServerEventType.ReceiveHandleError:
                        Log(e.Exception, LogType.Exception, LogLevels.Error);
                        break;
                }
            }
        }

        protected void Log(object message, LogType logType, LogLevels lowerLevel)
        {
            if ((LogLevels & lowerLevel) > 0)
            {
                if (logType == LogType.Log)
                {
                    Debug.Log($"[{nameof(NetworkServer)}] {message}");
                }
                else if (logType == LogType.Warning)
                {
                    Debug.LogWarning($"[{nameof(NetworkServer)}] {message}");
                }
                else if (logType == LogType.Error)
                {
                    Debug.LogError($"[{nameof(NetworkServer)}] {message}");
                }
                else if (logType == LogType.Exception)
                {
                    if (message is System.Net.Sockets.SocketException sexp)
                    {
                        Debug.LogError($"[{nameof(NetworkServer)}] Socket Exception({sexp.ErrorCode}): {sexp.Message}");
                    }
                    else
                    {
                        Debug.LogException(message as System.Exception);
                    }
                }
            }
        }
    }
}
