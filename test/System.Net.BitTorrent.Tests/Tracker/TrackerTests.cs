using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using System.Net.BitTorrent.Client.Tracker;
using System.Net.BitTorrent.Client;
using System.Threading;
using System.Net.BitTorrent.Common;

namespace System.Net.BitTorrent.Tracker
{
    public class TrackerFixture : IDisposable
    {
        Uri uri = new Uri("http://127.0.0.1:23456/");
        public System.Net.BitTorrent.Tracker.Listeners.HttpListener listener { get; private set; }
        public System.Net.BitTorrent.Tracker.Tracker server { get; private set; }

        public Uri Uri
        {
            get
            {
                return uri;
            }
        }

        public TrackerFixture()
        {
            listener = new System.Net.BitTorrent.Tracker.Listeners.HttpListener(Uri.OriginalString);
            listener.Start();
            server = new System.Net.BitTorrent.Tracker.Tracker();
            server.RegisterListener(listener);
            listener.Start();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }

    public class TrackerTests:IClassFixture<TrackerFixture>
    {
        //static void Main(string[] args)
        //{
        //    TrackerTests t = new TrackerTests();
        //    t.FixtureSetup();
        //    t.Setup();
        //    t.MultipleAnnounce();
        //    t.FixtureTeardown();
        //}
        //System.Net.BitTorrent.Client.Tracker.HTTPTracker tracker;
        TrackerFixture trackerFixture;
        public TrackerTests(TrackerFixture f)
        {
            trackerFixture = f;
        }

        public TrackerTests()
        {
            //tracker = new System.Net.BitTorrent.Client.Tracker.HTTPTracker(uri);
        }

        [Fact]
        public void MultipleAnnounce()
        {
            int announceCount = 0;
            Random r = new Random();
            ManualResetEvent handle = new ManualResetEvent(false);

            for (int i=0; i < 20; i++)
            {
                InfoHash infoHash = new InfoHash(new byte[20]);
                r.NextBytes(infoHash.Hash);
                TrackerTier tier = new TrackerTier(new string[] { trackerFixture.Uri.ToString() });
                tier.Trackers[0].AnnounceComplete += delegate {
                    if (++announceCount == 20)
                        handle.Set();
                };
                TrackerConnectionID id = new TrackerConnectionID(tier.Trackers[0], false, TorrentEvent.Started, new ManualResetEvent(false));
                System.Net.BitTorrent.Client.Tracker.AnnounceParameters parameters;
                parameters = new System.Net.BitTorrent.Client.Tracker.AnnounceParameters(0, 0, 0, TorrentEvent.Started,
                                                                       infoHash, false, new string('1', 20), "", 1411);
                tier.Trackers[0].Announce(parameters, id);
            }
#if IS_CORECLR
            Assert.True(handle.WaitOne(5000), "Some of the responses weren't received");

#else
           Assert.True(handle.WaitOne(5000, true), "Some of the responses weren't received");
#endif
        }
    }
}
