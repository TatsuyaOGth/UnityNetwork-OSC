using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Ogsn.Network
{
    using Internal;

    public class OscSender : MonoBehaviour
    {
        // Presets on Inspector
        public string SendHost = "127.0.0.1";
        public int SendPort = 50001;
        public Protocol Protocol = Protocol.UDP;
        public AutoRunType AutoRun = AutoRunType.Start;
        public UpdateType SendOn = UpdateType.Async;
        public bool IsOverwriteSameAddressOnFrame = false;
        public bool SendLog = false;
        public LogLevels LogLevels = LogLevels.Notice | LogLevels.Worning | LogLevels.Error;

        // Events
        public ClientEventHandler ClientEvent = new ClientEventHandler();

        // Properties
        public IClient Client => _client;
        public bool IsConnected => _client?.IsConnected ?? false;
        public bool IsConnectionRequested => _isConnectionRequested;

        // Internal data
        IClient _client;
        OscEncoder _encoder = new OscEncoder();
        bool _isConnectionRequested;
        Queue<OscMessage> _sendQueues = new Queue<OscMessage>();

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
            if (_client == null)
                return false;

            try
            {
                if (SendLog)
                {
                    PrintMessage(message);
                }

                var data = _encoder.Encode(message);
                return _client.Send(data, null);
                //TODO: response receive function.
            }
            catch (System.Exception exp)
            {
                Debug.LogException(exp);
                return false;
            }
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


        public void SendOnScheduler(string address, params object[] args)
        {
            SendOnScheduler(new OscMessage(address, args));
        }


        public void SendOnScheduler(OscMessage message)
        {
            if (SendOn == UpdateType.Async)
            {
                Send(message);
            }
            else
            {
                if (IsOverwriteSameAddressOnFrame)
                {
                    var elm = _sendQueues.FirstOrDefault(e => e.Address == message.Address);
                    if (elm == null)
                        _sendQueues.Enqueue(message);
                    else
                        elm = message;
                }
                else
                {
                    _sendQueues.Enqueue(message);
                }
            }
        }



        private void Awake()
        {
            if (AutoRun == AutoRunType.Awake)
            {
                Connect();
            }
        }

        private void Start()
        {
            if (AutoRun == AutoRunType.Start)
            {
                Connect();
            }
        }

        private void OnEnable()
        {
            if (AutoRun == AutoRunType.OnEnable)
            {
                Connect();
            }
        }

        private void OnDisable()
        {
            Disconnect();
        }

        private void OnDestroy()
        {
            Disconnect();
        }


        private void Update()
        {
            if (SendOn == UpdateType.Update && _sendQueues.Count > 0)
            {
                Flush();
            }
        }

        private void FixedUpdate()
        {
            if (SendOn == UpdateType.FixedUpdate && _sendQueues.Count > 0)
            {
                Flush();
            }
        }

        private void LateUpdate()
        {
            if (SendOn == UpdateType.LateUpdate && _sendQueues.Count > 0)
            {
                Flush();   
            }
        }


        void Flush()
        {
            int size = _sendQueues.Count;
            for (int i = 0; i < size; ++i)
            {
                Send(_sendQueues.Dequeue());
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