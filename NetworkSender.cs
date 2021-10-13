using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using UnityEngine;

namespace Ogsn.Network
{
    using Core;

    public class NetworkSender : NetworkClient
    {
        [Header("Sender Settings")]

        [Tooltip("Output to console when a message send")]
        public bool SendLog = false;

        // Events
        [Header("Event Handler")]
        public ClientEventHandler ClientEvent = new ClientEventHandler();

        // Properties

        public int NumberOfQueue =>
            _sendOnUpdateQueues.Count +
            _sendOnFixedUpdateQueues.Count +
            _sendOnLateUpdateQueues.Count;

        // Internal data
        object _lockObj = new object();
        Queue<byte[]> _sendOnUpdateQueues = new Queue<byte[]>();
        Queue<byte[]> _sendOnLateUpdateQueues = new Queue<byte[]>();
        Queue<byte[]> _sendOnFixedUpdateQueues = new Queue<byte[]>();


        public bool Send(byte[] data)
        {
            if (Client == null)
                return false;

            try
            {
                if (SendLog)
                {
                    Debug.Log($"[{nameof(NetworkSender)}] Send: {data.ToStringAsHex()}");
                }

                return Client.Send(data, null);
            }
            catch (System.Exception exp)
            {
                Log(exp, LogType.Exception, LogLevels.Error);
                return false;
            }
        }

        public bool Send(string text, Encoding encoding)
        {
            var data = encoding.GetBytes(text);
            return Send(data);
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
                    lock (_lockObj)
                    {
                        _sendOnUpdateQueues.Enqueue(data);
                    }
                    break;
                case UpdateCallbackType.FixedUpdate:
                    lock (_lockObj)
                    {
                        _sendOnFixedUpdateQueues.Enqueue(data);
                    }
                    break;
                case UpdateCallbackType.LateUpdate:
                    lock (_lockObj)
                    {
                        _sendOnLateUpdateQueues.Enqueue(data);
                    }
                    break;

                default:
                    Log($"Schedule type is '{schedule}', no sending", LogType.Warning, LogLevels.Worning);
                    break;
            }
        }

        private void Update()
        {
            Flush(_sendOnUpdateQueues);
        }

        private void FixedUpdate()
        {
            Flush(_sendOnFixedUpdateQueues);
        }

        private void LateUpdate()
        {
            Flush(_sendOnLateUpdateQueues);
        }

        void Flush(Queue<byte[]> queue)
        {
            if (queue.Count > 0)
            {
                lock (_lockObj)
                {
                    int len = queue.Count;
                    for (int i = 0; i < len; ++i)
                    {
                        Send(queue.Dequeue());
                    }
                }
            }
        }

        protected override void OnClientEventReceived(object sender, ClientEventArgs e)
        {
            base.OnClientEventReceived(sender, e);

            ClientEvent?.Invoke(e);
        }
    }
}
