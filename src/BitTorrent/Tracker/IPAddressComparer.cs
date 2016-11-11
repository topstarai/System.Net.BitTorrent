using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace System.Net.BitTorrent.Tracker
{
    public interface IPeerComparer
    {
        object GetKey(AnnounceParameters parameters);
    }

    public class IPAddressComparer : IPeerComparer
    {
        public object GetKey(AnnounceParameters parameters)
        {
            return parameters.ClientAddress;
        }
    }
}
