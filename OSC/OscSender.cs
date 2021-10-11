using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Ogsn.Network.OSC
{
    using Core;

    public class OscSender : MonoBehaviour
    {
        // Presets on Inspector
        [Header("Remote host")]
        public string SendHost = "127.0.0.1";
        public int SendPort = 50001;

        [Header("Advanced")]
        public Protocol Protocol = Protocol.UDP;
        public InitCallbackType AutoConnection = InitCallbackType.Start;
        public bool DisconnectOnDisable = true;

        [Tooltip("Overwrites the data before sending the message if the same address already exists in the queue.")]
        public bool Organize = false;

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
        OscEncoder _encoder = new OscEncoder();
        bool _isConnectionRequested;
        Queue<OscMessage> _sendOnUpdateQueues = new Queue<OscMessage>();
        Queue<OscMessage> _sendOnLateUpdateQueues = new Queue<OscMessage>();
        Queue<OscMessage> _sendOnFixedUpdateQueues = new Queue<OscMessage>();

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
                Debug.LogException(exp);
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
                Debug.LogException(exp);
            }
        }

        public bool Send(string address, params object[] args)
        {
            if (_client == null)
                return false;

            try
            {
                if (SendLog)
                {
                    PrintMessage(address, args);
                }

                var data = _encoder.Encode(address, args);
                return _client.Send(data, null);
                //TODO: response receive function.
            }
            catch (System.Exception exp)
            {
                Debug.LogException(exp);
                return false;
            }
        }


        public bool Send(OscMessage message)
        {
            return Send(message.Address, message.Data.ToArray());
        }

        public bool Send(OscBundle bundle)
        {
            if (_client == null)
                return false;

            try
            {
                if (SendLog)
                {
                    PrintMessage(bundle);
                }

                var data = _encoder.Encode(bundle);
                return _client.Send(data, null);
                //TODO: response receive function.
            }
            catch (System.Exception exp)
            {
                Debug.LogException(exp);
                return false;
            }
        }


        public void SendOnScheduler(string address, UpdateCallbackType schedule, params object[] args)
        {
            SendOnScheduler(new OscMessage(address, args), schedule);
        }


        public void SendOnScheduler(OscMessage message, UpdateCallbackType schedule)
        {
            switch (schedule)
            {
                case UpdateCallbackType.Async:
                    // Send immediately
                    Send(message);
                    break;
                case UpdateCallbackType.Update:
                    AddQueue(_sendOnUpdateQueues, message);
                    break;
                case UpdateCallbackType.FixedUpdate:
                    AddQueue(_sendOnFixedUpdateQueues, message);
                    break;
                case UpdateCallbackType.LateUpdate:
                    AddQueue(_sendOnLateUpdateQueues, message);
                    break;

                default:
                    if ((LogLevels & LogLevels.Worning) > 0)
                    {
                        Debug.LogWarning($"[{nameof(OscSender)}] Schedule type is '{schedule}', no sending");
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

        void AddQueue(Queue<OscMessage> queue, OscMessage message)
        {
            if (Organize)
            {
                var elm = queue.FirstOrDefault(e => e.Address == message.Address);
                if (elm == null)
                    queue.Enqueue(message);
                else
                    elm = message;
            }
            else
            {
                queue.Enqueue(message);
            }
        }

        void Flush(Queue<OscMessage> queue)
        {
            int size = queue.Count;
            for (int i = 0; i < size; ++i)
            {
                Send(queue.Dequeue());
            }
        }

        void PrintMessage(string address, params object[] args)
        {
            var sb = new StringBuilder();
            sb.Append(address);
            foreach (var val in args)
            {
                sb.Append(" ");
                if (val is byte[] b)
                    sb.Append(b.ToStringAsHex());
                else
                    sb.Append(val);
            }
            Debug.Log($"[{nameof(OscSender)}] Send: {sb}");
        }

        void PrintMessage(OscMessage message)
        {
            Debug.Log($"[{nameof(OscSender)}] Send: {message}");
        }

        void PrintMessage(OscBundle bundle)
        {
            Debug.Log($"[{nameof(OscSender)}] Send: {bundle}");
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
                            Debug.Log($"[{nameof(OscSender)}] {e.EventType}: Host={SendHost}:{SendPort}({Protocol})");
                        break;

                    case ClientEventType.Connected:
                    case ClientEventType.Disconnected:
                        if ((LogLevels & LogLevels.Notice) > 0)
                            Debug.Log($"[{nameof(OscSender)}] {e.EventType}: Host={SendHost}:{SendPort}({Protocol})");
                        break;

                    case ClientEventType.ConnectionError:
                        if ((LogLevels & LogLevels.Worning) > 0)
                            Debug.LogWarning($"[{nameof(OscSender)}] {e.EventType}: Host={SendHost}:{SendPort}({Protocol})");
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