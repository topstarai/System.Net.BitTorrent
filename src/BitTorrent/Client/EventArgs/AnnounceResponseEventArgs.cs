using System;
using System.Collections.Generic;
using System.Text;
using System.Net.BitTorrent.Client.Tracker;
using System.Net.BitTorrent.Common;

namespace System.Net.BitTorrent.Client.Tracker
{
    public class AnnounceResponseEventArgs : TrackerResponseEventArgs
    {
        List<Peer> peers;

        public List<Peer> Peers
        {
            get { return peers; }
        }

        public AnnounceResponseEventArgs(Tracker tracker, object state, bool successful)
            : this(tracker, state, successful, new List<Peer>())
        {

        }

        public AnnounceResponseEventArgs(Tracker tracker, object state, bool successful, List<Peer> peers)
            : base(tracker, state, successful)
        {
            this.peers = peers;
        }
    }
}
