using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

namespace Ogsn.Network
{
    using Internal;

    public class OscSender : MonoBehaviour
    {
        // Presets on Inspector
        public string SendHost = "127.0.0.1";
        public int SendPort = 50001;
        public Protocol Protocol = Protocol.UDP;
        public AutoRunType AutoRunType = AutoRunType.OnEnable;
        public bool PrintToConsole = false;

        // Event
        public OscSenderEventArgs OscSenderEvent;

        // Properties
        public bool IsConnected => _client?.IsConnected ?? false;
        public bool IsConnectionRequested => _isConnectionRequested;

        // Internal data
        IClient _client;
        OscEncoder _encoder = new OscEncoder();
        bool _isConnectionRequested;



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

        public void Send(string address, params object[] args)
        {
            if (_client == null)
                return;

            try
            {
                if (PrintToConsole)
                {
                    PrintMessage(address, args);
                }

                var data = _encoder.Encode(address, args);
                _client.Send(data, null);
                //TODO: response receive function.
            }
            catch (System.Exception exp)
            {
                Debug.LogException(exp);
            }
        }


        public void Send(OscMessage message)
        {
            if (_client == null)
                return;

            try
            {
                if (PrintToConsole)
                {
                    PrintMessage(message);
                }

                var data = _encoder.Encode(message);
                _client.Send(data, null);
                //TODO: response receive function.
            }
            catch (System.Exception exp)
            {
                Debug.LogException(exp);
            }
        }

        public void Send(OscBundle bundle)
        {
            if (_client == null)
                return;

            try
            {
                if (PrintToConsole)
                {
                    PrintMessage(bundle);
                }

                var data = _encoder.Encode(bundle);
                _client.Send(data, null);
                //TODO: response receive function.
            }
            catch (System.Exception exp)
            {
                Debug.LogException(exp);
            }
        }



        private void Awake()
        {
            if (AutoRunType == AutoRunType.Awake)
            {
                Connect();
            }
        }

        private void Start()
        {
            if (AutoRunType == AutoRunType.Start)
            {
                Connect();
            }
        }

        private void OnEnable()
        {
            if (AutoRunType == AutoRunType.OnEnable)
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
            ClientEventType type = requested ? ClientEventType.ConnectionRequested : ClientEventType.DisconnectionRequested;
            OscSenderEvent?.Invoke(this, ClientEventArgs.Info(type));
        }

        void OnClientEventReceived(object sender, ClientEventArgs e)
        {
            if (sender.Equals(_client))
            {
                switch (e.EventType)
                {
                    case ClientEventType.Connected:
                        Debug.Log($"[{nameof(OscSender)}] Connected: {SendHost}:{SendPort}");
                        break;

                    case ClientEventType.Disconnected:
                        Debug.Log($"[{nameof(OscSender)}] Disconnected: {SendHost}:{SendPort}");
                        break;

                    case ClientEventType.SendError:
                    case ClientEventType.ResponseError:
                        Debug.LogException(e.Exception);
                        break;
                }
                
                OscSenderEvent?.Invoke(this, e);
            }
        }
    }

    [System.Serializable]
    public class OscSenderEventArgs : UnityEvent<OscSender, ClientEventArgs> { }
}