using System;
using System.Text;
using System.Linq;

namespace Ogsn.Network.OSC
{
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
}
