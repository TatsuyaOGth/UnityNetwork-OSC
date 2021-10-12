using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Ogsn.Network.Core
{
    public class UDPServer : IServer
    {
        public Func<byte[], byte[]> ReceiveFunction { get; set; }

        public Protocol Protocol
        {
            get { return Protocol.UDP; }
        }

        public bool IsOpened
        {
            get { return _udpClient != null; }
        }

        public Encoding Encoding { get; set; } = Encoding.ASCII;

        // Events
        public event EventHandler<ServerEventArgs> NotifyServerEvent;


        // Internal data
        UdpClient _udpClient;
        Task _receiveTask;
        CancellationTokenSource _cancelTokenSource;
        int _lastListenPort;
        bool _willReopen = false;

        // Socket error code
        // https://docs.microsoft.com/en-us/windows/win32/winsock/windows-sockets-error-codes-2
        const int WSAEINTR = 10004;
        const int WSAETIMEDOUT = 10060;
        const int WSAENOTSOCK = 10038;
        const int WSAENOTCONN = 10057;


        public void Open(int listenPort)
        {
            // close if listener arrived
            if (_udpClient != null)
            {
                Close();
            }

            _lastListenPort = listenPort;

            // initialize receiver
            NotifyServerEvent?.Invoke(this, ServerEventArgs.Info(ServerEventType.Opening));
            var localEndPoint = new IPEndPoint(IPAddress.Any, listenPort);
            _udpClient = new UdpClient(localEndPoint);
            _udpClient.Client.ReceiveTimeout = 100;
            NotifyServerEvent?.Invoke(this, ServerEventArgs.Info(ServerEventType.Opened));

            // start receive thread
            _cancelTokenSource = new CancellationTokenSource();
            var cancelToken = _cancelTokenSource.Token;

            _receiveTask = Task.Run(() =>
            {
                NotifyServerEvent?.Invoke(this, ServerEventArgs.Info(ServerEventType.ReceiveThreadStarted));

                while (cancelToken.IsCancellationRequested == false)
                {
                    IPEndPoint endPoint = null;
                    byte[] data = null;

                    try
                    {
                        // waiting for data
                        //NotifyServerEvent?.Invoke(this, ServerEventArgs.Info(ServerEventType.WaitingForConnection));
                        data  = _udpClient.Receive(ref endPoint);

                        // is canccelled?
                        cancelToken.ThrowIfCancellationRequested();

                        // notify connected to client
                        NotifyServerEvent?.Invoke(this, ServerEventArgs.Info(ServerEventType.DataReceived));
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (SocketException exp)
                    {
                        if (exp.ErrorCode == WSAEINTR)
                        {
                            /* ignore this exception because this mean the listener stopped. */
                            break;
                        }
                        else if (exp.ErrorCode == WSAETIMEDOUT)
                        {
                            /* throgh this exception because this mean receiver timeouted. */
                            continue;
                        }
                        else if (exp.ErrorCode == WSAENOTSOCK || exp.ErrorCode == WSAENOTCONN)
                        {
                            // I was found this error on iOS device did sleep and wakeup.
                            _willReopen = true;
                            break;
                        }
                        else
                        {
                            var e = new Exception($"Socket Exception: code={exp.ErrorCode} {exp.Message}", exp);
                            NotifyServerEvent?.Invoke(this, ServerEventArgs.ReceiveError(e));
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        /*　ignore this exception because this mean the listener stopped. */
                        break;
                    }
                    catch (Exception exp)
                    {
                        NotifyServerEvent?.Invoke(this, ServerEventArgs.ReceiveError(exp));
                    }


                    if (endPoint != null && data != null)
                    {
                        try
                        {
                            // run response task
                            var res = ReceiveFunction?.Invoke(data);
                            if (res != null)
                            {
                                _udpClient.Send(res, res.Length, endPoint);
                                NotifyServerEvent?.Invoke(this, ServerEventArgs.Info(ServerEventType.ResponseSended));
                            }
                        }
                        catch (Exception exp)
                        {
                            NotifyServerEvent?.Invoke(this, ServerEventArgs.ReceiveHandleError(exp));
                        }
                    }
                }

                NotifyServerEvent?.Invoke(this, ServerEventArgs.Info(ServerEventType.ReceiveThreadStopped));

                if (_willReopen)
                {
                    _willReopen = false;
                    Close();
                    Open(_lastListenPort);
                }
            }, cancelToken);
        }

        public void Close()
        {
            if (_udpClient != null)
            {
                NotifyServerEvent?.Invoke(this, ServerEventArgs.Info(ServerEventType.Closing));

                // stop receive thread
                _cancelTokenSource.Cancel();
                //_receiveTask.Wait();
                _udpClient.Close();
                _udpClient.Dispose();
                _udpClient = null;

                NotifyServerEvent?.Invoke(this, ServerEventArgs.Info(ServerEventType.Closed));
            }
        }

        #region IDisposable Support
        public void Dispose()
        {
            Close();
        }
        #endregion
    }
}
