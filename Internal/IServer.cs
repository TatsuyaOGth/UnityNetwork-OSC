using System;
using System.Text;

namespace Ogsn.Network.Internal
{
    public interface IServer : IDisposable
    {
        void Open(int listenPort);
        void Close();

        bool isOpened { get; }
        Protocol protocol { get; }

        Encoding Encoding { get; set; }

        Func<byte[], byte[]> receiveFunction { get; set; }

        event EventHandler<ServerEventArgs> NotifyServerEvent;
    }
}
