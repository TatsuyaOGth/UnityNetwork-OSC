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
        public int ListenPort = 50000;
        public Protocol Protocol = Protocol.UDP;
        public AutoRunType AutoRun = AutoRunType.Start;
        public UpdateType UpdateType = UpdateType.Update;
        public bool IsOverwriteSameAddressOnFrame = true;
        public bool ReceiveLog = false;
        public LogLevels LogLevels = LogLevels.Notice | LogLevels.Worning | LogLevels.Error;

        // Unity Event
        public OscMessageReceivedArgs OscMessageReceived = new OscMessageReceivedArgs();
        public ServerEventHandler ServerEvent = new ServerEventHandler();

        // Properties
        public IServer Server => _server;
        public bool IsOpened => _server?.IsOpened ?? false;
        

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

            if (UpdateType != UpdateType.None)
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
            if (AutoRun == AutoRunType.Awake)
            {
                Open();
            }
        }

        private void Start()
        {
            if (AutoRun == AutoRunType.Start)
            {
                Open();
            }
        }

        private void OnEnable()
        {
            if (AutoRun == AutoRunType.OnEnable)
            {
                Open();
            }
        }

        private void OnDisable()
        {
            Close();
        }

        private void OnDestroy()
        {
            Close();
        }

        private void Update()
        {
            if (UpdateType == UpdateType.Update && _queue.Count > 0)
            {
                lock (_lockObj)
                {
                    OscMessageReceived.Invoke(this, _queue.Dequeue());
                }
            }
        }

        private void FixedUpdate()
        {
            if (UpdateType == UpdateType.FixedUpdate && _queue.Count > 0)
            {
                lock (_lockObj)
                {
                    OscMessageReceived.Invoke(this, _queue.Dequeue());
                }
            }
        }

        private void LateUpdate()
        {
            if (UpdateType == UpdateType.LateUpdate && _queue.Count > 0)
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

                    if (UpdateType == UpdateType.Async)
                    {
                        OscMessageReceived.Invoke(this, m);
                    }
                    else
                    {
                        lock (_lockObj)
                        {
                            if (IsOverwriteSameAddressOnFrame)
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
