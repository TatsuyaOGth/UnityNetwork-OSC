using System;
using System.Text;

namespace Ogsn.Network.Core
{
    public interface IServer : IDisposable
    {
        void Open(int listenPort);
        void Close();

        bool IsOpened { get; }
        Protocol Protocol { get; }

        Encoding Encoding { get; set; }

        Func<byte[], byte[]> ReceiveFunction { get; set; }

        event EventHandler<ServerEventArgs> NotifyServerEvent;
    }
}
