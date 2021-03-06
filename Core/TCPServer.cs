using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

namespace Ogsn.Network.Core
{
    public class TCPServer : IServer
    {
        public Func<byte[], byte[]> ReceiveFunction { get; set; }

        public Protocol Protocol
        {
            get { return Protocol.TCP; }
        }

        public bool IsOpened
        {
            get { return _tcpListener != null; }
        }

        public Encoding Encoding { get; set; } = Encoding.ASCII;

        // Events
        public event EventHandler<ServerEventArgs> NotifyServerEvent;

        TcpListener _tcpListener;
        Task _receiveTask;
        List<Task> _streamingTasks = new List<Task>();
        CancellationTokenSource _cancelTokenSource;


        // Socket error code
        // https://docs.microsoft.com/en-us/windows/win32/winsock/windows-sockets-error-codes-2
        const int WSAEINTR = 10004;
        const int WSAEWOULDBLOCK = 10035;


        public void Open(int listenPort)
        {
            // close if listener arrived
            if (_tcpListener != null)
            {
                Close();
            }

            // initialize receiver
            NotifyServerEvent?.Invoke(this, ServerEventArgs.Info(ServerEventType.Opening));
            var endPoint = new IPEndPoint(IPAddress.Any, listenPort);
            _tcpListener = new TcpListener(endPoint);
            _tcpListener.Start();
            NotifyServerEvent?.Invoke(this, ServerEventArgs.Info(ServerEventType.Opened));

            // start receive thread
            _cancelTokenSource = new CancellationTokenSource();
            _receiveTask = Task.Run(() => ReceiveTask(_cancelTokenSource.Token));
        }

        public void Close()
        {
            if (_tcpListener != null)
            {
                NotifyServerEvent?.Invoke(this, ServerEventArgs.Info(ServerEventType.Closing));

                _cancelTokenSource?.Cancel();

                // waiting for streaming thread end
                if (_streamingTasks.Count > 0)
                {
                    try
                    {
                        Task.WaitAll(_streamingTasks.ToArray(), 1000);
                        _streamingTasks.ForEach(task => task.Dispose());
                        _streamingTasks.Clear();
                    }
                    catch (Exception exp)
                    {
                        NotifyServerEvent?.Invoke(this, ServerEventArgs.Disconnected(exp));
                    }
                }

                // waiting for receiver thread end
                _tcpListener?.Stop();
                _receiveTask.Wait();
                _receiveTask.Dispose();
                _receiveTask = null;
                _tcpListener = null;

                NotifyServerEvent?.Invoke(this, ServerEventArgs.Info(ServerEventType.Closed));
            }
        }

        void StreamingTask(TcpClient remoteClient, CancellationToken cancelToken)
        {
            // get stream to remote host
            using var stream = remoteClient.GetStream();

            byte[] buffer = new byte[remoteClient.ReceiveBufferSize];

            while (cancelToken.IsCancellationRequested == false)
            {
                byte[] receivedData = null;

                try
                {
                    // read data
                    receivedData = NetworkStreamIO.ReadData(stream, Encoding);
                    NotifyServerEvent?.Invoke(this, ServerEventArgs.DataReceived(receivedData));

                    // is canccelled?
                    cancelToken.ThrowIfCancellationRequested();
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (EndOfStreamException exp)
                {
                    NotifyServerEvent?.Invoke(this, ServerEventArgs.Disconnected(exp));
                    break;
                }
                catch (SocketException exp)
                {
                    if (exp.ErrorCode == WSAEWOULDBLOCK)
                        continue;
                }
                catch (Exception exp)
                {
                    if (cancelToken.IsCancellationRequested)
                        break;

                    NotifyServerEvent?.Invoke(this, ServerEventArgs.ReceiveError(exp));
                }


                try
                {
                    if (receivedData != null)
                    {
                        var res = ReceiveFunction?.Invoke(receivedData);
                        if (res != null)
                        {
                            NetworkStreamIO.WriteData(stream, res, Encoding);
                            stream.Flush();
                            NotifyServerEvent?.Invoke(this, ServerEventArgs.ResponseSended(res));
                        }
                    }
                }
                catch (Exception exp)
                {
                    if (cancelToken.IsCancellationRequested)
                        break;

                    NotifyServerEvent?.Invoke(this, ServerEventArgs.ReceiveHandleError(exp));
                }
            }
        }


        void ReceiveTask(CancellationToken cancelToken)
        {
            NotifyServerEvent?.Invoke(this, ServerEventArgs.Info(ServerEventType.ReceiveThreadStarted));

            while (cancelToken.IsCancellationRequested == false)
            {
                try
                {
                    // waiting for client...
                    NotifyServerEvent?.Invoke(this, ServerEventArgs.Info(ServerEventType.WaitingForConnection));
                    var remoteClient = _tcpListener.AcceptTcpClient();

                    // canceled?
                    cancelToken.ThrowIfCancellationRequested();

                    // connected to client
                    NotifyServerEvent?.Invoke(this, ServerEventArgs.Info(ServerEventType.Connected));

                    // start new streaming task
                    var streamingTask = Task.Run(() => StreamingTask(remoteClient, cancelToken), cancelToken);
                    _streamingTasks.Add(streamingTask);
                }
                catch (SocketException exp)
                {
                    if (cancelToken.IsCancellationRequested)
                        break;

                    switch (exp.ErrorCode)
                    {
                        case WSAEINTR:
                            /*this exception ignored because stop listener. */
                            break;
                        default:
                            var e = new Exception($"Socket Exception: code={exp.ErrorCode}, message={exp.Message}", exp);
                            NotifyServerEvent?.Invoke(this, ServerEventArgs.ReceiveError(e));
                            break;
                    }
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception exp)
                {
                    if (cancelToken.IsCancellationRequested)
                        break;

                    NotifyServerEvent?.Invoke(this, ServerEventArgs.ReceiveError(exp));
                }
            }

            NotifyServerEvent?.Invoke(this, ServerEventArgs.Info(ServerEventType.ReceiveThreadStopped));
        }

        #region IDisposable Support
        public void Dispose()
        {
            Close();
        }
        #endregion
    }
}
