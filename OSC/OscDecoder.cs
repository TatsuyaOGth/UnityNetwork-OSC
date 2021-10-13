using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace Ogsn.Network.OSC
{
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
}
