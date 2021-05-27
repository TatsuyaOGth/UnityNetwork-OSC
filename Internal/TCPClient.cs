using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;


namespace Ogsn.Network.Internal
{
    public class TCPClient : IClient
    {
        // Events
        public event EventHandler<ClientEventArgs> NotifyClientEvent;

        // Properties
        public bool IsConnected
        {
            get
            {
                if (_tcpClient != null && _stream != null)
                {
                    //try
                    //{
                    //    WriteData(_stream, new byte[] { 0x04 });
                    //}
                    //catch { return false; }
                    return _tcpClient.Connected;
                }
                return false;
            }
        }

        public Protocol Protocol
        {
            get => Protocol.TCP;
        }

        public int ResponseTimeout { get; set; } = 2000;

        public Encoding Encoding { get; set; } = Encoding.ASCII;


        // Internal data
        TcpClient _tcpClient;
        IPEndPoint _targetEndPoint;
        NetworkStream _stream;
        Task _connectionTask;
        CancellationTokenSource _cancellationTokenSource;

        bool _isConnectionIntermittently;
        int _retryConnectionInterval = 1000;


        public void Connect(string host, int port)
        {
            if (IsConnected)
                return;

            NotifyClientEvent?.Invoke(this, ClientEventArgs.Info(ClientEventType.Connecting));

            // set connection status
            _isConnectionIntermittently = true;
            _targetEndPoint = new IPEndPoint(IPAddress.Parse(host), port);
            _cancellationTokenSource = new CancellationTokenSource();

            // start connection intermittently loop
            if (_connectionTask == null || _connectionTask.IsCompleted)
                _connectionTask = ConnectionLoop(_cancellationTokenSource.Token);
        }

        public void Disconnect()
        {
            _isConnectionIntermittently = false;

            if (IsConnected == false)
            {
                return;
            }


            NotifyClientEvent?.Invoke(this, ClientEventArgs.Info(ClientEventType.Disconnecting));

            // Stop connection loop thread
            _cancellationTokenSource?.Cancel();

            // close stream
            _stream?.Close();
            _stream?.Dispose();
            _stream = null;

            _tcpClient?.Close();
            _tcpClient?.Dispose();
            _tcpClient = null;

            NotifyClientEvent?.Invoke(this, ClientEventArgs.Info(ClientEventType.Disconnected));
        }

        public bool Send(byte[] data, Action<byte[]> getAction)
        {
            if (!IsConnected)
                return false;

            try
            {
                WriteData(_stream, data);
                NotifyClientEvent?.Invoke(this, ClientEventArgs.Info(ClientEventType.Sended));
            }
            catch (Exception exp)
            {
                NotifyClientEvent?.Invoke(this, ClientEventArgs.SendError(exp));
                return false;
            }

            // Wait for response async
            if (getAction != null)
            {
                _ = Task.Run(() =>
                {
                    try
                    {
                        var res = ReadData(_stream);
                        NotifyClientEvent?.Invoke(this, ClientEventArgs.Info(ClientEventType.ResponseReceived));
                        getAction(res);
                    }
                    catch (Exception exp)
                    {
                        NotifyClientEvent?.Invoke(this, ClientEventArgs.ResponseError(exp));
                    }
                });
            }
            return true;
        }

        byte[] ReadData(Stream stream)
        {
            using var reader = new BinaryReader(stream, Encoding, true);

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
        }

        void WriteData(Stream stream, byte[] data)
        {
            using var writer = new BinaryWriter(stream, Encoding, true);

            // write data length
            writer.Write((uint)data.Length);

            // write data
            writer.Write(data);
        }

        async Task ConnectionLoop(CancellationToken cancellationToken)
        {
            do
            {
                try
                {
                    if (_tcpClient == null || _tcpClient.Connected == false)
                    {
                        try
                        {
                            // Cleate new TCP client instance
                            _tcpClient = new TcpClient();
                            await _tcpClient.ConnectAsync(_targetEndPoint.Address, _targetEndPoint.Port);
                            NotifyClientEvent?.Invoke(this, ClientEventArgs.Info(ClientEventType.Connected));

                            // Get stream
                            if (_stream != null)
                            {
                                _stream?.Close();
                                _stream?.Dispose();
                                _stream = null;
                            }
                            _stream = _tcpClient.GetStream();
                            _stream.ReadTimeout = ResponseTimeout;
                        }
                        catch (Exception exp)
                        {
                            NotifyClientEvent?.Invoke(this, ClientEventArgs.ConnectionError(exp));
                        }
                    }

                    await Task.Delay(_retryConnectionInterval, cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
            while (cancellationToken.IsCancellationRequested == false && _isConnectionIntermittently);
        }

        #region IDisposable Support
        public void Dispose()
        {
            Disconnect();
        }
        #endregion
    }
}
