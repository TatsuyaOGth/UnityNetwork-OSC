using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

namespace Ogsn.Network
{
    using Core;

    public class NetworkClient : MonoBehaviour
    {
        // Presets on Inspector
        [Header("Remote host")]
        public string SendHost = "127.0.0.1";
        public int SendPort = 50000;

        [Header("Advanced")]
        public Protocol Protocol = Protocol.UDP;
        public InitCallbackType AutoConnection = InitCallbackType.Start;
        public bool DisconnectOnDisable = true;
        public LogLevels LogLevels = LogLevels.Notice | LogLevels.Worning | LogLevels.Error;

        // Properties
        public IClient Client => _client;
        public bool IsConnected => _client?.IsConnected ?? false;
        public bool IsConnectionRequested => _isConnectionRequested;

        // Internal data
        IClient _client;
        bool _isConnectionRequested;

        public void Connect()
        {
            if (_client != null)
                return;

            try
            {
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
                Log(exp, LogType.Exception, LogLevels.Error);
            }
        }

        public void Disconnect()
        {
            if (_client == null)
                return;

            try
            {
                _client.Disconnect();
                _client.NotifyClientEvent -= OnClientEventReceived;
                _client.Dispose();
                _client = null;
            }
            catch (System.Exception exp)
            {
                Log(exp, LogType.Exception, LogLevels.Error);
            }
        }

        public void Awake()
        {
            if (AutoConnection == InitCallbackType.Awake)
            {
                Connect();
            }
        }

        public void Start()
        {
            if (AutoConnection == InitCallbackType.Start)
            {
                Connect();
            }
        }

        public void OnEnable()
        {
            if (AutoConnection == InitCallbackType.OnEnable)
            {
                Connect();
            }
        }

        public void OnDisable()
        {
            if (DisconnectOnDisable)
            {
                Disconnect();
            }
        }

        public void OnDestroy()
        {
            Disconnect();
        }


        protected virtual void OnClientEventReceived(object sender, ClientEventArgs e)
        {
            if (sender.Equals(_client))
            {
                switch (e.EventType)
                {
                    case ClientEventType.Connecting:
                        _isConnectionRequested = true;
                        Log($"{e.EventType}: Host={SendHost}:{SendPort}({Protocol})", LogType.Log, LogLevels.Verbose);
                        break;

                    case ClientEventType.Disconnecting:
                        _isConnectionRequested = false;
                        Log($"{e.EventType}: Host={SendHost}:{SendPort}({Protocol})", LogType.Log, LogLevels.Verbose);
                        break;

                    case ClientEventType.Sended:
                    case ClientEventType.ResponseReceived:
                        Log($"{e.EventType}: Host={SendHost}:{SendPort}({Protocol})", LogType.Log, LogLevels.Verbose);
                        break;

                    case ClientEventType.Connected:
                    case ClientEventType.Disconnected:
                        Log($"{e.EventType}: Host={SendHost}:{SendPort}({Protocol})", LogType.Log, LogLevels.Notice);
                        break;

                    case ClientEventType.ConnectionError:
                        Log($"{e.EventType}: Host={SendHost}:{SendPort}({Protocol})", LogType.Warning, LogLevels.Worning);
                        break;

                    case ClientEventType.SendError:
                    case ClientEventType.ResponseError:
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
                    Debug.Log($"[{nameof(NetworkClient)}] {message}");
                }
                else if (logType == LogType.Warning)
                {
                    Debug.LogWarning($"[{nameof(NetworkClient)}] {message}");
                }
                else if (logType == LogType.Error)
                {
                    Debug.LogError($"[{nameof(NetworkClient)}] {message}");
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
