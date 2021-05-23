using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace Ogsn.Network.Internal
{
    public class PingClient : IDisposable
    {
        Ping _ping = new Ping();

        public async Task<PingReply> SendAsync(string host)
        {
            int timeout = 2000;
            var reply = await _ping.SendPingAsync(host, timeout);
            return reply;
        }


        #region IDisposable Support

        public void Dispose()
        {
            _ping?.Dispose();
            _ping = null;
        }
        #endregion
    }
}
