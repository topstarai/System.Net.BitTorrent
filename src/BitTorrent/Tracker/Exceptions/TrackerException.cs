using System;
using System.Collections.Generic;
using System.Text;

namespace System.Net.BitTorrent.Tracker
{
    public class TrackerException : Exception
    {
        public TrackerException()
            : base()
        {
        }

        public TrackerException(string message)
            : base(message)
        {
        }
    }
}
