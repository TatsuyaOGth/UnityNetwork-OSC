using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ogsn.Network
{
    using Core;

    public class NetworkReceiver : NetworkServer
    {
        [Header("Receiver Settings")]

        [Tooltip("Timing to invoke the event handler after receiving the message.")]
        public UpdateCallbackType UpdateType = UpdateCallbackType.Update;

        [Tooltip("Output to console when a message received")]
        public bool ReceiveLog = false;

        // Unity Event
        [Header("Event Handler")]
        public DataReceivedEventHandler DataReceived = new DataReceivedEventHandler();
        public ServerEventHandler ServerEvent = new ServerEventHandler();

        // Properties
        public int NumberOfQueue => _queue.Count;

        // Internal data
        readonly Queue<byte[]> _queue = new Queue<byte[]>();
        readonly object _lockObj = new object();


        public bool HasReceivedData
        {
            get => _queue.Count > 0;
        }

        public byte[] GetNextData()
        {
            if (!HasReceivedData)
                return null;

            if (UpdateType != UpdateCallbackType.None)
            {
                Log($"[{nameof(NetworkReceiver)}] The other messages may have been invoked on \"DataReceived\". Set \"UpdateType = None\" to stop this.", LogType.Warning, LogLevels.Worning);
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
                        DataReceived.Invoke(_queue.Dequeue());
                    }
                }
            }
        }

        private byte[] OnDataReceived(byte[] data)
        {
            if (data.Length > 0)
            {
                if (ReceiveLog)
                {
                    Debug.Log($"[{nameof(NetworkReceiver)}] Received: {data.ToStringAsHex()}");
                }

                if (UpdateType == UpdateCallbackType.Async)
                {
                    DataReceived.Invoke(data);
                }
                else
                {
                    lock (_lockObj)
                    {
                        _queue.Enqueue(data);
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
