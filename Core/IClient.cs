using System;
using System.Text;

namespace Ogsn.Network.Core
{
    public interface IClient : IDisposable
    {
        void Connect(string host, int port);
        void Disconnect();

        bool Send(byte[] data, Action<byte[]> getAction);

        bool IsConnected { get; }
        Protocol Protocol { get; }

        int ResponseTimeout { set; get; }
        Encoding Encoding { get; set; }

        event EventHandler<ClientEventArgs> NotifyClientEvent;
    }
}
