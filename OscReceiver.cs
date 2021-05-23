using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Ogsn.Network
{
    using Internal;

    public class OscReceiver : MonoBehaviour
    {
        // Presets on Inspector
        public int ListenPort = 50000;
        public Protocol Protocol = Protocol.UDP;
        public AutoRunType AutoRunType = AutoRunType.OnEnable;
        public bool PrintToConsole = false;
        public UpdateType UpdateType = UpdateType.Update;

        // Properties
        public bool IsOpened => _server?.isOpened ?? false;

        // Event
        public OscMessageReceivedArgs OscReceivedMessageEvent;
        public OscReceiverEventArgs OscReceiverEvent;

        // Internal data
        IServer _server;
        OscDecoder _decoder = new OscDecoder();
        Queue<OscMessage> _queue = new Queue<OscMessage>();

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

            _server.receiveFunction = OnDataReceived;
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

            return _queue.Dequeue();
        }


        private void Awake()
        {
            if (AutoRunType == AutoRunType.Awake)
            {
                Open();
            }
        }

        private void Start()
        {
            if (AutoRunType == AutoRunType.Start)
            {
                Open();
            }
        }

        private void OnEnable()
        {
            if (AutoRunType == AutoRunType.OnEnable)
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
                OscReceivedMessageEvent.Invoke(this, _queue.Dequeue());
            }
        }

        private void FixedUpdate()
        {
            if (UpdateType == UpdateType.FixedUpdate && _queue.Count > 0)
            {
                OscReceivedMessageEvent.Invoke(this, _queue.Dequeue());
            }
        }

        byte[] OnDataReceived(byte[] data)
        {
            if (data.Length > 0)
            {
                var msgs = _decoder.Decode(data);
                foreach (var m in msgs)
                {
                    if (PrintToConsole)
                    {
                        Debug.Log($"[{nameof(OscReceiver)}] Received: {m}");
                    }

                    if (UpdateType == UpdateType.Async)
                    {
                        OscReceivedMessageEvent.Invoke(this, m);
                    }
                    else
                    {
                        _queue.Enqueue(m);
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
                    case ServerEventType.Opened:
                        Debug.Log($"[{nameof(OscReceiver)}] Opened: {ListenPort}");
                        break;

                    case ServerEventType.Closed:
                        Debug.Log($"[{nameof(OscReceiver)}] Closed: {ListenPort}");
                        break;

                    case ServerEventType.ReceiveError:
                    case ServerEventType.ReceiveHandleError:
                        Debug.LogException(e.Exception);
                        break;
                }

                OscReceiverEvent?.Invoke(this, e);
            }
        }
    }

    [System.Serializable]
    public class OscMessageReceivedArgs : UnityEvent<OscReceiver, OscMessage> { }

    [System.Serializable]
    public class OscReceiverEventArgs : UnityEvent<OscReceiver, ServerEventArgs> { }
}
