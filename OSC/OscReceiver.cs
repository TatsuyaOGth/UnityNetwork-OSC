using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ogsn.Network.OSC
{
    using Core;

    public class OscReceiver : NetworkServer
    {
        [Header("Receiver Settings")]

        [Tooltip("Timing to invoke the event handler after receiving the message.")]
        public UpdateCallbackType UpdateType = UpdateCallbackType.Update;

        [Tooltip("Overwrites the data after receiving the message if the same address already exists in the queue.")]
        public bool Organize = false;

        [Tooltip("Output to console when a message received")]
        public bool ReceiveLog = false;

        // Unity Event
        [Header("Event Handler")]
        public OscMessageReceivedEventHandler OscMessageReceived = new OscMessageReceivedEventHandler();
        public ServerEventHandler ServerEvent = new ServerEventHandler();

        // Properties
        public int NumberOfQueue => _queue.Count;


        // Internal data
        readonly OscDecoder _decoder = new OscDecoder();
        readonly Queue<OscMessage> _queue = new Queue<OscMessage>();
        readonly object _lockObj = new object();


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
                Log($"[{nameof(OscReceiver)}] The other messages may have been invoked on \"OscMessageReceived\". Set \"UpdateType = None\" to stop this.", LogType.Warning, LogLevels.Worning);
            }

            lock (_lockObj)
            {
                return _queue.Dequeue();
            }
        }

        private void Update()
        {
            if (UpdateType == UpdateCallbackType.Update && _queue.Count > 0)
            {
                Flush();
            }
        }

        private void FixedUpdate()
        {
            if (UpdateType == UpdateCallbackType.FixedUpdate && _queue.Count > 0)
            {
                Flush();
            }
        }

        private void LateUpdate()
        {
            if (UpdateType == UpdateCallbackType.LateUpdate && _queue.Count > 0)
            {
                Flush();
            }
        }

        private void Flush()
        {
            if (_queue.Count > 0)
            {
                lock (_lockObj)
                {
                    int len = _queue.Count;
                    for (int i = 0; i < len; ++i)
                    {
                        OscMessageReceived.Invoke(_queue.Dequeue());
                    }
                }
            }
        }

        private byte[] OnDataReceived(byte[] data)
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
                        OscMessageReceived.Invoke(m);
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

        protected override void OnServerEventReceived(object sender, ServerEventArgs e)
        {
            base.OnServerEventReceived(sender, e);

            if (e.EventType == ServerEventType.Opened)
            {
                Server.ReceiveFunction = OnDataReceived;
            }

            ServerEvent?.Invoke(e.EventType);
        }
    }
}
