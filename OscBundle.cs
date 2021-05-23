using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ogsn.Network
{
    /// <summary>
    /// OSC Bundle Class
    /// </summary>
    public class OscBundle
    {
        public long Timestamp { get; set; }
        public List<object> Data { get; private set; } = new List<object>();

        public OscBundle()
        {
            Timestamp = 0x1u;
        }

        public OscBundle(long timestamp)
        {
            this.Timestamp = timestamp;
        }

        public void Add(OscMessage message)
        {
            Data.Add(message);
        }

        public void Add(OscBundle bundle)
        {
            Data.Add(bundle);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("#bundle");
            foreach (var val in Data)
            {
                sb.Append(" ");
                sb.Append(val);
            }
            return sb.ToString();
        }
    }
}