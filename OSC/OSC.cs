using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace Ogsn.Network.OSC
{
    #region OSC Parser class
    /// <summary>
    /// Simple OSC Decoder(Parser)
    /// </summary>
    public class OscDecoder
    {
        #region Internal data

        readonly List<OscMessage> _messageList = new List<OscMessage>(256);
        byte[] _readBuffer;
        int _readPoint;

        #endregion

        #region Public methods

        public OscMessage[] Decode(byte[] data)
        {
            _messageList.Clear();
            _readBuffer = data;
            _readPoint = 0;
            ReadMessage();
            _readBuffer = null;
            return _messageList.ToArray();
        }

        #endregion

        #region Internal methods

        void ReadMessage()
        {
            var path = ReadString();
            if (path == "#bundle")
            {
                // TODO: Read timestamp
                _ = ReadInt64();

                while (true)
                {
                    if (_readPoint >= _readBuffer.Length)
                    {
                        return;
                    }
                    var peek = _readBuffer[_readPoint];
                    if (peek == '/' || peek == '#')
                    {
                        ReadMessage();
                        return;
                    }
                    var bundleEnd = _readPoint + ReadInt32();
                    while (_readPoint < bundleEnd)
                    {
                        ReadMessage();
                    }
                }
            }

            var temp = new OscMessage
            {
                Address = path
            };

            var types = ReadString();
            temp.Data = new List<object>(types.Length - 1);

            for (var i = 0; i < types.Length - 1; i++)
            {
                switch (types[i + 1])
                {
                    case 'i':
                        temp.Add(ReadInt32());
                        break;
                    case 'f':
                        temp.Add(ReadFloat32());
                        break;
                    case 's':
                        temp.Add(ReadString());
                        break;
                    case 'b':
                        temp.Add(ReadBlob());
                        break;
                    case 'h':
                        temp.Add(ReadInt64());
                        break;
                }
            }

            _messageList.Add(temp);
        }

        float ReadFloat32()
        {
            byte[] temp = {
                _readBuffer [_readPoint + 3],
                _readBuffer [_readPoint + 2],
                _readBuffer [_readPoint + 1],
                _readBuffer [_readPoint]
            };
            _readPoint += 4;
            return BitConverter.ToSingle(temp, 0);
        }

        int ReadInt32()
        {
            int temp =
                (_readBuffer[_readPoint + 0] << 24) +
                (_readBuffer[_readPoint + 1] << 16) +
                (_readBuffer[_readPoint + 2] << 8) +
                (_readBuffer[_readPoint + 3]);
            _readPoint += 4;
            return temp;
        }

        long ReadInt64()
        {
            long temp =
                ((long)_readBuffer[_readPoint + 0] << 56) +
                ((long)_readBuffer[_readPoint + 1] << 48) +
                ((long)_readBuffer[_readPoint + 2] << 40) +
                ((long)_readBuffer[_readPoint + 3] << 32) +
                ((long)_readBuffer[_readPoint + 4] << 24) +
                ((long)_readBuffer[_readPoint + 5] << 16) +
                ((long)_readBuffer[_readPoint + 6] << 8) +
                ((long)_readBuffer[_readPoint + 7]);
            _readPoint += 8;
            return temp;
        }

        string ReadString()
        {
            var offset = 0;
            while (_readBuffer[_readPoint + offset] != 0)
            {
                offset++;
            }
            var s = Encoding.UTF8.GetString(_readBuffer, _readPoint, offset);
            _readPoint += (offset + 4) & ~3;
            return s;
        }

        byte[] ReadBlob()
        {
            var length = ReadInt32();
            var temp = new byte[length];
            Array.Copy(_readBuffer, _readPoint, temp, 0, length);
            _readPoint += (length + 3) & ~3;
            return temp;
        }

        #endregion
    }
    #endregion

    #region OSC Encoder class
    /// <summary>
    /// Simple OSC Encoder
    /// </summary>
    public class OscEncoder
    {
        #region Internal data

        readonly byte[] _buffer;
        int _readPoint;
        readonly float[] _tmpFloat = new float[1];
        readonly byte[] _tmpByte = new byte[4];
        readonly StringBuilder _stringBuilder = new StringBuilder(64);

        #endregion

        #region Constructer

        public OscEncoder(int bufferSize = 8192)
        {
            _buffer = new byte[bufferSize];
        }

        #endregion

        #region Public methods

        public byte[] Encode(string address, params object[] args)
        {
            Initialize();
            SetAddress(address);
            SetTgas(args);
            foreach (var arg in args)
                Append(arg);
            return _buffer.Take(_readPoint).ToArray();
        }

        public byte[] Encode(OscMessage oscMessage)
        {
            return Encode(oscMessage.Address, oscMessage.Data.ToArray());
        }

        public byte[] Encode(OscBundle oscBundle)
        {
            Initialize();
            AppendAsString("#bundle");
            AppendAsInt64(oscBundle.Timestamp);

            var encoder = new OscEncoder(2048);

            foreach (var data in oscBundle.Data)
            {
                byte[] messageBlock = null;
                if (data is OscMessage msg)
                    messageBlock = encoder.Encode(msg);
                else if (data is OscBundle bundle)
                    messageBlock = encoder.Encode(bundle);

                if (messageBlock == null)
                    throw new FormatException($"OSCEncoder failed read data in bundle: {data.GetType().ToString()}");

                AppendAsInt32(messageBlock.Length);
                Array.Copy(messageBlock, 0, _buffer, _readPoint, messageBlock.Length);
                _readPoint = _readPoint + messageBlock.Length;
            }
            return _buffer.Take(_readPoint).ToArray();
        }

        #endregion

        #region Internal methods

        void Initialize()
        {
            _readPoint = 0;
        }

        void SetAddress(string address)
        {
            AppendAsString(address);
        }

        void SetTgas(object[] args)
        {
            _stringBuilder.Clear();
            _stringBuilder.Append(",");
            foreach (var arg in args)
            {
                _stringBuilder.Append(GetTag(arg));
            }
            AppendAsString(_stringBuilder.ToString());
        }

        char GetTag(object val)
        {
            if (val is int)
                return 'i';
            else if (val is float)
                return 'f';
            else if (val is string)
                return 's';
            else if (val is byte[])
                return 'b';
            else if (val is long)
                return 'h';
            else
                throw new FormatException($"OSCEncoder is not support value type: {val.GetType()}");
        }

        void Append(object val)
        {
            if (val is int i)
                AppendAsInt32(i);
            else if (val is float f)
                AppendAsFloat(f);
            else if (val is string s)
                AppendAsString(s);
            else if (val is byte[] b)
                AppendAsBytes(b);
            else if (val is long l)
                AppendAsInt64(l);
            else
                throw new FormatException($"OSCEncoder is not support value type: {val.GetType()}");
        }

        void AppendAsString(string data)
        {
            var len = data.Length;
            for (var i = 0; i < len; i++)
                _buffer[_readPoint++] = (byte)data[i];

            var len4 = Align4(len + 1);
            for (var i = len; i < len4; i++)
                _buffer[_readPoint++] = 0;
        }

        void AppendAsInt32(int data)
        {
            _buffer[_readPoint++] = (byte)(data >> 24);
            _buffer[_readPoint++] = (byte)(data >> 16);
            _buffer[_readPoint++] = (byte)(data >> 8);
            _buffer[_readPoint++] = (byte)(data);
        }

        void AppendAsInt64(long data)
        {
            _buffer[_readPoint++] = (byte)(data >> 56);
            _buffer[_readPoint++] = (byte)(data >> 48);
            _buffer[_readPoint++] = (byte)(data >> 40);
            _buffer[_readPoint++] = (byte)(data >> 32);
            _buffer[_readPoint++] = (byte)(data >> 24);
            _buffer[_readPoint++] = (byte)(data >> 16);
            _buffer[_readPoint++] = (byte)(data >> 8);
            _buffer[_readPoint++] = (byte)(data);
        }

        void AppendAsFloat(float data)
        {
            _tmpFloat[0] = data;
            System.Buffer.BlockCopy(_tmpFloat, 0, _tmpByte, 0, 4);
            _buffer[_readPoint++] = _tmpByte[3];
            _buffer[_readPoint++] = _tmpByte[2];
            _buffer[_readPoint++] = _tmpByte[1];
            _buffer[_readPoint++] = _tmpByte[0];
        }

        void AppendAsBytes(byte[] data)
        {
            int len = data.Length;
            AppendAsInt32(len);

            for (var i = 0; i < len; i++)
                _buffer[_readPoint++] = data[i];

            var len4 = Align4(len + 1);
            for (var i = len; i < len4; i++)
                _buffer[_readPoint++] = 0;
        }


        int Align4(int length)
        {
            return (length + 3) & ~3;
        }

        #endregion
    }

    #endregion
}
