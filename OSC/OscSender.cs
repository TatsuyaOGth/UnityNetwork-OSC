using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Ogsn.Network.OSC
{
    using Core;

    public class OscSender : NetworkClient
    {
        [Header("Sender Settings")]

        [Tooltip("Overwrites the data before sending the message if the same address already exists in the queue.")]
        public bool Organize = false;

        [Tooltip("Output to console when a message sending")]
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
        OscEncoder _encoder = new OscEncoder();
        Queue<OscMessage> _sendOnUpdateQueues = new Queue<OscMessage>();
        Queue<OscMessage> _sendOnLateUpdateQueues = new Queue<OscMessage>();
        Queue<OscMessage> _sendOnFixedUpdateQueues = new Queue<OscMessage>();

        
        public bool Send(string address, params object[] args)
        {
            if (Client == null)
                return false;

            try
            {
                if (SendLog)
                {
                    PrintMessage(address, args);
                }

                var data = _encoder.Encode(address, args);
                return Client.Send(data, null);
                //TODO: response receive function.
            }
            catch (System.Exception exp)
            {
                Log(exp, LogType.Exception, LogLevels.Error);
                return false;
            }
        }


        public bool Send(OscMessage message)
        {
            return Send(message.Address, message.Data.ToArray());
        }

        public bool Send(OscBundle bundle)
        {
            if (Client == null)
                return false;

            try
            {
                if (SendLog)
                {
                    PrintMessage(bundle);
                }

                var data = _encoder.Encode(bundle);
                return Client.Send(data, null);
                //TODO: response receive function.
            }
            catch (System.Exception exp)
            {
                Log(exp, LogType.Exception, LogLevels.Error);
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

        protected override void OnClientEventReceived(object sender, ClientEventArgs e)
        {
            base.OnClientEventReceived(sender, e);

            ClientEvent?.Invoke(e.EventType);
        }
    }
}