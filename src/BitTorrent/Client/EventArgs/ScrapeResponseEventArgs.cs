using System;
using System.Collections.Generic;
using System.Text;
using System.Net.BitTorrent.Client.Tracker;

namespace System.Net.BitTorrent.Client.Tracker
{
    public class ScrapeResponseEventArgs : TrackerResponseEventArgs
    {
        public ScrapeResponseEventArgs(Tracker tracker, object state, bool successful)
            : base(tracker, state, successful)
        {

        }
    }
}
