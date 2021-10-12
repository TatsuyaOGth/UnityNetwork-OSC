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

        public Text LogText;

        public void SenderConnect()
        {
            Sender.Connect();
        }

        public void SenderDisconnect()
        {
            Sender.Disconnect();
        }

        public void Send()
        {
            Sender.Send(AddressInput.text, StringInput.text);
        }



        public void ReceiverOpen()
        {
            Receiver.Open();
        }

        public void ReceiverClose()
        {
            Receiver.Close();
        }




        // Log

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
        }

        private void Update()
        {
            LogText.text = _sb.ToString();
        }

        public void ClearLog()
        {
            _sb.Clear();
        }
    }
}
