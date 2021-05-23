using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;

namespace Ogsn.Network.Internal
{
    public class TCPServer : IServer
    {
        public Func<byte[], byte[]> receiveFunction { get; set; }

        public Protocol protocol
        {
            get { return Protocol.TCP; }
        }

        public bool isOpened
        {
            get { return tcpListener != null; }
        }

        public Encoding Encoding { get; set; } = Encoding.ASCII;

        // Events
        public event EventHandler<ServerEventArgs> NotifyServerEvent;

        TcpListener tcpListener;
        Task receiveTask;
        CancellationTokenSource cancelTokenSource;


        // Socket error code
        // https://docs.microsoft.com/en-us/windows/win32/winsock/windows-sockets-error-codes-2
        const int WSAEINTR = 10004;



        public void Open(int listenPort)
        {
            // close if listener arrived
            if (tcpListener != null)
            {
                Close();
            }

            // start receive thread
            cancelTokenSource = new CancellationTokenSource();
            receiveTask = Task.Run(() => ReceiveTask(listenPort, cancelTokenSource.Token));
        }

        public void Close()
        {
            if (tcpListener != null)
            {
                NotifyServerEvent?.Invoke(this, ServerEventArgs.Info(ServerEventType.Closing));
                // stop receive thread
                cancelTokenSource?.Cancel();
                tcpListener?.Stop();

                // waiting for the thread stopped
                receiveTask.Wait();
                receiveTask.Dispose();
                receiveTask = null;
                NotifyServerEvent?.Invoke(this, ServerEventArgs.Info(ServerEventType.Closed));
            }
        }

        byte[] ReadData(NetworkStream stream)
        {
            using (var reader = new BinaryReader(stream, Encoding, true))
            {
                // read data length
                var length = reader.ReadInt32();

                // read data
                byte[] buffer = new byte[length];
                int readPosition = 0;
                do
                {
                    var readData = reader.ReadBytes(length);
                    Array.Copy(readData, 0, buffer, readPosition, readData.Length);
                    readPosition += readData.Length;
                }
                while (readPosition < length);
                return buffer;
                throw new Exception("Tcp format not defined.");
            }
        }

        void WriteData(NetworkStream stream, byte[] data)
        {
            using (var writer = new BinaryWriter(stream, Encoding, true))
            {
                // write data length
                writer.Write((uint)data.Length);
                // write data
                writer.Write(data);
            }
        }

        async Task StreamingTask(TcpClient remoteClient, CancellationToken cancelToken)
        {
            await Task.Run((Action)(() =>
            {
                // get stream to remote host
                using (var stream = remoteClient.GetStream())
                {
                    byte[] buffer = new byte[remoteClient.ReceiveBufferSize];

                    while (cancelToken.IsCancellationRequested == false)
                    {
                        byte[] receivedData = null;

                        try
                        {
                            // read data
                            receivedData = ReadData(stream);
                            NotifyServerEvent?.Invoke(this, ServerEventArgs.Info(ServerEventType.DataReceived));

                            // is canccelled?
                            cancelToken.ThrowIfCancellationRequested();
                        }
                        catch (TaskCanceledException)
                        {
                            break;
                        }
                        catch (Exception exp)
                        {
                            NotifyServerEvent?.Invoke(this, ServerEventArgs.ReceiveError(exp));
                            break;
                        }


                        try
                        {
                            // send response
                            var res = receiveFunction?.Invoke(receivedData);
                            if (res != null)
                            {
                                WriteData(stream, res);
                                stream.Flush();
                                NotifyServerEvent?.Invoke(this, ServerEventArgs.Info(ServerEventType.ResponseSended));
                            }
                        }
                        catch (Exception exp)
                        {
                            NotifyServerEvent?.Invoke(this, ServerEventArgs.ReceiveHandleError(exp));
                        }
                    }
                }
            }));
        }


        void ReceiveTask(int listenPort, CancellationToken cancelToken)
        {
            NotifyServerEvent?.Invoke(this, ServerEventArgs.Info(ServerEventType.ReceiveThreadStarted));

            try
            {
                NotifyServerEvent?.Invoke(this, ServerEventArgs.Info(ServerEventType.Opening));
                var endPoint = new IPEndPoint(IPAddress.Any, listenPort);
                tcpListener = new TcpListener(endPoint);
                tcpListener.Start();
                NotifyServerEvent?.Invoke(this, ServerEventArgs.Info(ServerEventType.Opened));

                while (cancelToken.IsCancellationRequested == false)
                {
                    try
                    {
                        // waiting for client...
                        NotifyServerEvent?.Invoke(this, ServerEventArgs.Info(ServerEventType.WaitingForConnection));
                        var remoteClient = tcpListener.AcceptTcpClient();

                        // connected to client
                        NotifyServerEvent?.Invoke(this, ServerEventArgs.Info(ServerEventType.Connected));

                        // start new streaming task
                        _ = StreamingTask(remoteClient, cancelToken);
                    }
                    catch (SocketException exp)
                    {
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
                    catch (Exception exp)
                    {
                        NotifyServerEvent?.Invoke(this, ServerEventArgs.ReceiveError(exp));
                    }
                }
            }
            catch (Exception exp)
            {
                NotifyServerEvent?.Invoke(this, ServerEventArgs.ReceiveError(exp));
            }
            finally
            {
                tcpListener.Stop();
                tcpListener = null;

                cancelTokenSource.Dispose();
                cancelTokenSource = null;
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
