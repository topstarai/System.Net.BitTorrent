#if !DISABLE_DHT
using System;
using System.Collections.Generic;
using System.Text;
using System.Net.BitTorrent.Client;
using System.Net;
using System.Net.BitTorrent.Common;

namespace System.Net.BitTorrent.Dht.Listeners
{
    public delegate void MessageReceived(byte[] buffer, IPEndPoint endpoint);

    public class DhtListener : UdpListenerBase
    {
        public event MessageReceived MessageReceived;

        public DhtListener(IPEndPoint endpoint)
            : base(endpoint)
        {

        }

        protected override void OnMessageReceived(byte[] buffer, IPEndPoint endpoint)
        {
            MessageReceived h = MessageReceived;
            if (h != null)
                h(buffer, endpoint);
        }
    }
}
#endif