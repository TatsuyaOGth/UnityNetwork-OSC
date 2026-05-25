using System;
using System.Net.Sockets;
using System.IO;
using System.Text;

namespace Ogsn.Network.Core
{
    public static class NetworkStreamIO
    {
        public static byte[] ReadData(NetworkStream stream, Encoding encoding)
        {
            using var reader = new BinaryReader(stream, encoding, true);

            // read data length
            var length = reader.ReadInt32();

            // read data
            byte[] buffer = new byte[length];
            int readPosition = 0;
            while (readPosition < length)
            {
                int remain = length - readPosition;
                var readData = reader.ReadBytes(remain);
                if (readData.Length == 0)
                {
                    throw new EndOfStreamException("Unexpected end of stream while reading payload.");
                }

                Buffer.BlockCopy(readData, 0, buffer, readPosition, readData.Length);
                readPosition += readData.Length;
            }

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
