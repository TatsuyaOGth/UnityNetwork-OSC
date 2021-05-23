using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ogsn.Network
{
    /// <summary>
    /// OSC Message Class
    /// </summary>
    public class OscMessage
    {
        public string Address { get; set; }
        public List<object> Data { get; set; } = new List<object>();

        public OscMessage() { }

        public OscMessage(string address, params object[] data) : this()
        {
            this.Address = address;
            this.Data = data.ToList();
        }

        public void Add(object data)
        {
            this.Data.Add(data);
        }

        public T Get<T>(int index)
        {
            return (T)Data.ElementAtOrDefault(index);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(Address);
            foreach (var val in Data)
            {
                sb.Append(" ");
                if (val is byte[] b)
                    sb.Append(b.ToStringAsHex());
                else
                    sb.Append(val);
            }
            return sb.ToString();
        }
    }
}
