using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Ogsn.Network.OSC;

namespace Ogsn.Network.Example
{
    public class OscExample : MonoBehaviour
    {
        public OscSender Sender;
        public OscReceiver Receiver;

        public InputField AddressInput;
        public InputField StringInput;

        public Text ReceivedMessage;

        public Text LogText;



        // Connect and disconnect the sender on a script

        public void SenderConnect()
        {
            Sender.Connect();
        }

        public void SenderDisconnect()
        {
            Sender.Disconnect();
        }



        // Open and close the receiver on a script

        public void ReceiverOpen()
        {
            Receiver.Open();
        }

        public void ReceiverClose()
        {
            Receiver.Close();
        }



        // Data sending

        public void Send()
        {
            // Send one string data
            Sender.Send(AddressInput.text, StringInput.text);

            // Multiple data example
            /*
            Sender.Send("/data", 100, 1.2f, "text");
            */

            // Using OscMessage object example
            /*
            var m = new OscMessage();
            m.Address = "/data";
            m.Add(100);
            m.Add(1.2f);
            m.Add("text");
            Sender.Send(m);
            */
        }



        // Data receiving

        // - Pull style (you need set 'Update Type == None' to the receiver)
        private void Update()
        {
            if (Receiver.HasReceivedMessages)
            {
                var m = Receiver.GetNextMessage();
                ReceivedMessage.text = $"{m.Address} {m.Get<string>(0)}";
            }
        }

        // - Push style (you need set 'Update Type != None' to the receiver)
        public void OnReceivedMessage(OscMessage m)
        {
            ReceivedMessage.text = $"{m.Address} {m.Get<string>(0)}";
        }



        // Show log

        StringBuilder _sb = new StringBuilder();

        private void Awake()
        {
            Application.logMessageReceivedThreaded += (condition, stackTrace, type) =>
            {
                if (type == LogType.Warning)
                    _sb.AppendLine($"<color=yellow>{condition}</color>");
                else if (type == LogType.Error || type == LogType.Exception)
                    _sb.AppendLine($"<color=red>{condition}</color>");
                else
                    _sb.AppendLine($"<color=white>{condition}</color>");
            };

            StartCoroutine(LogToTextCoroutine());
        }

        public void ClearLog()
        {
            _sb.Clear();
        }

        IEnumerator LogToTextCoroutine()
        {
            while (Application.isPlaying)
            {
                LogText.text = _sb.ToString();
                yield return null;
            }
        }
    }
}
