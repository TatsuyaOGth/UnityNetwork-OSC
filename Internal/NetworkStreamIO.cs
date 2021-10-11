using System;
using System.Net.Sockets;
using System.IO;
using System.Text;

namespace Ogsn.Network.Internal
{
    public static class NetworkStreamIO
    {
        const int HeaderLength = sizeof(int);
        static readonly byte[] _lengthReadBuffer = new byte[HeaderLength];

        public static byte[] ReadData(NetworkStream stream, Encoding encoding)
        {
            using var reader = new BinaryReader(stream, encoding, true);

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

        public static void WriteData(NetworkStream stream, byte[] data, Encoding encoding)
        {
            using var writer = new BinaryWriter(stream, encoding, true);

            // write data length
            writer.Write(data.Length);

            // write data
            writer.Write(data);
        }
    }
}
