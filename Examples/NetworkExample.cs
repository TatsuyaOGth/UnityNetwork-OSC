using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ogsn.Network.Example
{
    public class NetworkExample : MonoBehaviour
    {
        public NetworkSender Sender;
        public NetworkReceiver Receiver;

        public InputField SendMessageInput;

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
            Sender.Send(SendMessageInput.text, System.Text.Encoding.ASCII);
        }



        public void ReceiverOpen()
        {
            Receiver.Open();
        }

        public void ReceiverClose()
        {
            ReceiverClose();
        }
    }
}
