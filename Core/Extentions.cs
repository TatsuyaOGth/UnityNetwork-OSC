using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Ogsn.Network.Core
{
    public static class Extentions
    {
        /// <summary>
        /// byte配列を16進数表現された文字列に変換する
        /// </summary>
        /// <param name="bytes">Byte Array</param>
        /// <returns>String</returns>
        public static string ToStringAsHex(this byte[] bytes)
        {
            var sb = new StringBuilder(bytes.Length * 2);
            sb.Append("0x");
            for (int i = 0; i < bytes.Length; i++)
            {
                sb.Append($"{bytes[i]:X2}");
            }
            return sb.ToString();
        }
    }
}
