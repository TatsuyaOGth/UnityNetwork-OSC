using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

namespace Ogsn.Network.Core
{
    public class UDPClient : IClient
    {
        // Events
        public event EventHandler<ClientEventArgs> NotifyClientEvent;

        // Properties
        public bool IsConnected
        {
            get => _udpClient != null;
        }

        public Protocol Protocol
        {
            get => Protocol.UDP;
        }

        public int ResponseTimeout { get; set; } = 2000;

        public Encoding Encoding { get; set; } = Encoding.ASCII;

        // Internal data
        UdpClient _udpClient;
        IPEndPoint _targetEndPoint;
        CancellationTokenSource _cancellationTokenSource;

        
        public void Connect(string host, int port)
        {
            if (IsConnected)
                return;

            NotifyClientEvent?.Invoke(this, ClientEventArgs.Info(ClientEventType.Connecting));

            // create client instance with auto binding to any local endpoint
            _targetEndPoint = new IPEndPoint(IPAddress.Parse(host), port);
            _udpClient = new UdpClient();
            _udpClient.Connect(_targetEndPoint);
            _cancellationTokenSource = new CancellationTokenSource();

            // Set response receive timeout
            _udpClient.Client.ReceiveTimeout = ResponseTimeout;

            NotifyClientEvent?.Invoke(this, ClientEventArgs.Info(ClientEventType.Connected));
        }

        public void Disconnect()
        {
            if (IsConnected == false)
                return;

            NotifyClientEvent?.Invoke(this, ClientEventArgs.Info(ClientEventType.Disconnecting));

            // Cancel task
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
            }

            // close and dispose client
            _udpClient.Close();
            _udpClient.Dispose();
            _udpClient = null;

            NotifyClientEvent?.Invoke(this, ClientEventArgs.Info(ClientEventType.Disconnected));
        }

        public bool Send(byte[] data, Action<byte[]> getAction)
        {
            if (!IsConnected)
                return false;

            // Send async
            try
            {
                _udpClient.Send(data, data.Length);
                NotifyClientEvent?.Invoke(this, ClientEventArgs.DataSended(data));
            }
            catch (Exception exp)
            {
                NotifyClientEvent?.Invoke(this, ClientEventArgs.SendError(data, exp));
                return false;
            }

            // Wait for response async
            if (getAction != null)
            {
                var token = _cancellationTokenSource.Token;
                _ = Task.Run(() =>
                {
                    try
                    {
                        IPEndPoint remoteEP = null;
                        var res = _udpClient.Receive(ref remoteEP);
                        token.ThrowIfCancellationRequested();
                        NotifyClientEvent?.Invoke(this, ClientEventArgs.ResponseReceived(res));
                        getAction(res);
                    }
                    catch (TaskCanceledException)
                    {
                            // Task was canceled.
                    }
                    catch (Exception exp)
                    {
                        NotifyClientEvent?.Invoke(this, ClientEventArgs.ResponseError(exp));
                    }
                }, token);
            }
            return true;
        }

        #region IDisposable Support
        public void Dispose()
        {
            Disconnect();
        }
        #endregion
    }
}
