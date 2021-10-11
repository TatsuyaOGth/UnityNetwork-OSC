using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ogsn.Network.OSC
{
    using Core;

    public class OscReceiver : MonoBehaviour
    {
        // Presets on Inspector
        [Header("Listen port")]
        public int ListenPort = 50000;

        [Header("Advanced")]
        public Protocol Protocol = Protocol.UDP;
        public InitCallbackType AutoOpen = InitCallbackType.Start;

        [Tooltip("Timing to invoke the event handler after receiving the message.")]
        public UpdateCallbackType UpdateType = UpdateCallbackType.Update;

        public bool CloseOnDisable = true;

        [Tooltip("Overwrites the data after receiving the message if the same address already exists in the queue.")]
        public bool Organize = false;

        [Tooltip("Output to console when a message received")]
        public bool ReceiveLog = false;

        public LogLevels LogLevels = LogLevels.Notice | LogLevels.Worning | LogLevels.Error;

        // Unity Event
        [Header("Event Handler")]
        public OscMessageReceivedArgs OscMessageReceived = new OscMessageReceivedArgs();
        public ServerEventHandler ServerEvent = new ServerEventHandler();

        // Properties
        public IServer Server => _server;
        public bool IsOpened => _server?.IsOpened ?? false;
        public int NumberOfQueue => _queue.Count;


        // Internal data
        IServer _server;
        readonly OscDecoder _decoder = new OscDecoder();
        readonly Queue<OscMessage> _queue = new Queue<OscMessage>();
        readonly object _lockObj = new object();

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

            _server.ReceiveFunction = OnDataReceived;
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

        public bool HasReceivedMessages
        {
            get => _queue.Count > 0;
        }

        public OscMessage GetNextMessage()
        {
            if (!HasReceivedMessages)
                return null;

            if (UpdateType != UpdateCallbackType.None)
            {
                Debug.LogWarning($"[{nameof(OscReceiver)}] The other messages may have been invoked on \"OscReceivedMessageEvent\". Set \"UpdateType = None\" to stop this.");
            }

            lock (_lockObj)
            {
                return _queue.Dequeue();
            }
        }


        private void Awake()
        {
            if (AutoOpen == InitCallbackType.Awake)
            {
                Open();
            }
        }

        private void Start()
        {
            if (AutoOpen == InitCallbackType.Start)
            {
                Open();
            }
        }

        private void OnEnable()
        {
            if (AutoOpen == InitCallbackType.OnEnable)
            {
                Open();
            }
        }

        private void OnDisable()
        {
            if (CloseOnDisable)
            {
                Close();
            }
        }

        private void OnDestroy()
        {
            Close();
        }

        private void Update()
        {
            if (UpdateType == UpdateCallbackType.Update && _queue.Count > 0)
            {
                lock (_lockObj)
                {
                    OscMessageReceived.Invoke(this, _queue.Dequeue());
                }
            }
        }

        private void FixedUpdate()
        {
            if (UpdateType == UpdateCallbackType.FixedUpdate && _queue.Count > 0)
            {
                lock (_lockObj)
                {
                    OscMessageReceived.Invoke(this, _queue.Dequeue());
                }
            }
        }

        private void LateUpdate()
        {
            if (UpdateType == UpdateCallbackType.LateUpdate && _queue.Count > 0)
            {
                lock (_lockObj)
                {
                    OscMessageReceived.Invoke(this, _queue.Dequeue());
                }
            }
        }

        byte[] OnDataReceived(byte[] data)
        {
            if (data.Length > 0)
            {
                var msgs = _decoder.Decode(data);
                foreach (var m in msgs)
                {
                    if (ReceiveLog)
                    {
                        Debug.Log($"[{nameof(OscReceiver)}] Received: {m}");
                    }

                    if (UpdateType == UpdateCallbackType.Async)
                    {
                        OscMessageReceived.Invoke(this, m);
                    }
                    else
                    {
                        lock (_lockObj)
                        {
                            if (Organize)
                            {
                                var elm = _queue.FirstOrDefault(e => e.Address == m.Address);
                                if (elm == null)
                                    _queue.Enqueue(m);
                                else
                                    elm = m;
                            }
                            else
                            {
                                _queue.Enqueue(m);
                            }
                        }
                    }
                }
            }

            //TODO: send response function.
            return null;
        }

        void OnServerEventReceived(object sender, ServerEventArgs e)
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
                        if ((LogLevels & LogLevels.Verbose) > 0)
                            Debug.Log($"[{nameof(OscReceiver)}] {e.EventType}: Listen port={ListenPort}({Protocol})");
                        break;

                    case ServerEventType.Opened:
                    case ServerEventType.Closed:
                        if((LogLevels & LogLevels.Notice) > 0)
                            Debug.Log($"[{nameof(OscReceiver)}] {e.EventType}: Listen port={ListenPort}({Protocol})");
                        break;

                    case ServerEventType.Disconnected:
                        if ((LogLevels & LogLevels.Worning) > 0)
                            Debug.LogWarning($"[{nameof(OscReceiver)}] {e.EventType}, {e?.Exception?.Message}: Listen port={ListenPort}({Protocol})");
                        break;

                    case ServerEventType.ReceiveError:
                    case ServerEventType.ReceiveHandleError:
                        if ((LogLevels & LogLevels.Error) > 0)
                            Debug.LogException(e.Exception);
                        break;
                }

                ServerEvent?.Invoke(e.EventType);
            }
        }
    }
}
