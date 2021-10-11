using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

namespace Ogsn.Network
{
    using Core;

    public class NetworkSender : MonoBehaviour
    {
        // Presets on Inspector
        [Header("Remote host")]
        public string SendHost = "127.0.0.1";
        public int SendPort = 50000;

        [Header("Advanced")]
        public Protocol Protocol = Protocol.UDP;
        public InitCallbackType AutoConnection = InitCallbackType.Start;
        public bool DisconnectOnDisable = true;

        [Tooltip("Output to console when a message send")]
        public bool SendLog = false;

        public LogLevels LogLevels = LogLevels.Notice | LogLevels.Worning | LogLevels.Error;

        // Events
        [Header("Event Handler")]
        public ClientEventHandler ClientEvent = new ClientEventHandler();

        // Properties
        public IClient Client => _client;
        public bool IsConnected => _client?.IsConnected ?? false;
        public bool IsConnectionRequested => _isConnectionRequested;
        public int NumberOfQueue =>
            _sendOnUpdateQueues.Count +
            _sendOnFixedUpdateQueues.Count +
            _sendOnLateUpdateQueues.Count;

        // Internal data
        IClient _client;
        bool _isConnectionRequested;
        Queue<byte[]> _sendOnUpdateQueues = new Queue<byte[]>();
        Queue<byte[]> _sendOnLateUpdateQueues = new Queue<byte[]>();
        Queue<byte[]> _sendOnFixedUpdateQueues = new Queue<byte[]>();

        public void Connect()
        {
            if (_client != null)
                return;

            try
            {
                SetIsConnectionRequested(true);

                switch (Protocol)
                {
                    case Protocol.UDP:
                        _client = new UDPClient();
                        break;
                    case Protocol.TCP:
                        _client = new TCPClient();
                        break;
                    default:
                        throw new System.ArgumentException("The protocol is not defined");
                }

                _client.NotifyClientEvent += OnClientEventReceived;
                _client.Connect(SendHost, SendPort);
            }
            catch (System.Exception exp)
            {
                if ((LogLevels & LogLevels.Error) > 0)
                {
                    Debug.LogException(exp);
                }
            }
        }

        public void Disconnect()
        {
            if (_client == null)
                return;

            try
            {
                SetIsConnectionRequested(false);

                _client.Disconnect();
                _client.NotifyClientEvent -= OnClientEventReceived;
                _client.Dispose();
                _client = null;
            }
            catch (System.Exception exp)
            {
                if ((LogLevels & LogLevels.Error) > 0)
                {
                    Debug.LogException(exp);
                }
            }
        }


        public bool Send(byte[] data, UnityAction<byte[]> responseAction = null)
        {
            if (_client == null)
                return false;

            try
            {
                if (SendLog)
                {
                    Debug.Log($"[{nameof(NetworkSender)}] Send: {data.ToStringAsHex()}");
                }

                if (responseAction != null)
                {
                    return _client.Send(data, e => responseAction(e));
                }
                else
                {
                    return _client.Send(data, null);
                }
            }
            catch (System.Exception exp)
            {
                if ((LogLevels & LogLevels.Error) > 0)
                {
                    Debug.LogException(exp);
                }
                return false;
            }
        }

        public bool Send(string text, Encoding encoding, UnityAction<byte[]> responseAction = null)
        {
            var data = encoding.GetBytes(text);
            return Send(data, responseAction);
        }

        public void SendOnScheduler(byte[] data, UpdateCallbackType schedule)
        {
            switch (schedule)
            {
                case UpdateCallbackType.Async:
                    // Send immediately
                    Send(data);
                    break;
                case UpdateCallbackType.Update:
                    _sendOnUpdateQueues.Enqueue(data);
                    break;
                case UpdateCallbackType.FixedUpdate:
                    _sendOnFixedUpdateQueues.Enqueue(data);
                    break;
                case UpdateCallbackType.LateUpdate:
                    _sendOnLateUpdateQueues.Enqueue(data);
                    break;

                default:
                    if ((LogLevels & LogLevels.Worning) > 0)
                    {
                        Debug.LogWarning($"[{nameof(NetworkSender)}] Schedule type is '{schedule}', no sending");
                    }
                    break;
            }
        }



        private void Awake()
        {
            if (AutoConnection == InitCallbackType.Awake)
            {
                Connect();
            }
        }

        private void Start()
        {
            if (AutoConnection == InitCallbackType.Start)
            {
                Connect();
            }
        }

        private void OnEnable()
        {
            if (AutoConnection == InitCallbackType.OnEnable)
            {
                Connect();
            }
        }

        private void OnDisable()
        {
            if (DisconnectOnDisable)
            {
                Disconnect();
            }
        }

        private void OnDestroy()
        {
            Disconnect();
        }


        private void Update()
        {
            Flush(_sendOnUpdateQueues);
        }

        private void FixedUpdate()
        {
            Flush(_sendOnUpdateQueues);
        }

        private void LateUpdate()
        {
            Flush(_sendOnLateUpdateQueues);
        }

        void Flush(Queue<byte[]> queue)
        {
            int size = queue.Count;
            for (int i = 0; i < size; ++i)
            {
                Send(queue.Dequeue(), null);
            }
        }

        void SetIsConnectionRequested(bool requested)
        {
            _isConnectionRequested = requested;
        }

        void OnClientEventReceived(object sender, ClientEventArgs e)
        {
            if (sender.Equals(_client))
            {
                switch (e.EventType)
                {
                    case ClientEventType.ConnectionRequested:
                    case ClientEventType.DisconnectionRequested:
                    case ClientEventType.Connecting:
                    case ClientEventType.Disconnecting:
                    case ClientEventType.Sended:
                    case ClientEventType.ResponseReceived:
                        if ((LogLevels & LogLevels.Verbose) > 0)
                            Debug.Log($"[{nameof(NetworkSender)}] {e.EventType}: Host={SendHost}:{SendPort}({Protocol})");
                        break;

                    case ClientEventType.Connected:
                    case ClientEventType.Disconnected:
                        if ((LogLevels & LogLevels.Notice) > 0)
                            Debug.Log($"[{nameof(NetworkSender)}] {e.EventType}: Host={SendHost}:{SendPort}({Protocol})");
                        break;

                    case ClientEventType.ConnectionError:
                        if ((LogLevels & LogLevels.Worning) > 0)
                            Debug.LogWarning($"[{nameof(NetworkSender)}] {e.EventType}: Host={SendHost}:{SendPort}({Protocol})");
                        break;

                    case ClientEventType.SendError:
                    case ClientEventType.ResponseError:
                        if ((LogLevels & LogLevels.Error) > 0)
                            Debug.LogException(e.Exception);
                        break;
                }

                ClientEvent?.Invoke(e.EventType);
            }
        }
    }
}
