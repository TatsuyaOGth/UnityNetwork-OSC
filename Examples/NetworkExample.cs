using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Ogsn.Network;

namespace Ogsn.Network.Example
{
    public class NetworkExample : MonoBehaviour
    {
        public NetworkSender Sender;
        public NetworkReceiver Receiver;

        public InputField SendMessageInput;

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
            // Send string with encoding
            Sender.Send(SendMessageInput.text, System.Text.Encoding.ASCII);

            // Send byte[]
            /*
            byte[] data = System.Text.Encoding.ASCII.GetBytes(SendMessageInput.text);
            Sender.Send(data);
            */
        }




        // Data receiving

        // - Pull style (you need set 'Update Type == None' to the receiver)
        private void Update()
        {
            if (Receiver.HasReceivedData)
            {
                var data = Receiver.GetNextData();
                ReceivedMessage.text = System.Text.Encoding.ASCII.GetString(data);
            }
        }

        // - Push style (you need set 'Update Type != None' to the receiver)
        public void OnReceivedData(byte[] data)
        {
            ReceivedMessage.text = System.Text.Encoding.ASCII.GetString(data);
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
