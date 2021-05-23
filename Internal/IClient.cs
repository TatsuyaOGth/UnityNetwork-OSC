using System;
using System.Collections.Generic;
using System.Text;

namespace Ogsn.Network.Internal
{
    interface IClient : IDisposable
    {
        void Connect(string host, int port);
        void Disconnect();

        void Send(byte[] data, Action<byte[]> getAction);

        bool IsConnected { get; }
        Protocol Protocol { get; }

        int ResponseTimeout { set; get; }
        Encoding Encoding { get; set; }

        event EventHandler<ClientEventArgs> NotifyClientEvent;
    }
}
