using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using System.Net.BitTorrent.Client.Tracker;
using System.Net.BitTorrent.Common;
using System.Threading;

namespace System.Net.BitTorrent.Client
{
    public class HttpTrackerFixture : IDisposable
    {
        public System.Net.BitTorrent.Tracker.Tracker Server { get; private set; }
        public System.Net.BitTorrent.Tracker.Listeners.HttpListener Listener { get; private set; }
        string prefix = "http://localhost:47124/announce/";
        public List<string> Keys { get; private set; }
        public HttpTrackerFixture()
        {
            Keys = new List<string>();
            Server = new System.Net.BitTorrent.Tracker.Tracker();
            Server.AllowUnregisteredTorrents = true;
            Listener = new System.Net.BitTorrent.Tracker.Listeners.HttpListener(prefix);
            Listener.AnnounceReceived += delegate (object o, System.Net.BitTorrent.Tracker.AnnounceParameters e) {
                Keys.Add(e.Key);
            };
            Server.RegisterListener(Listener);

            Listener.Start();
        }

        public void Dispose()
        {
            Listener.Stop();
            Server.Dispose();
        }
    }

    public class HttpTrackerTests:IClassFixture<HttpTrackerFixture>
    {
        //static void Main()
        //{
        //    HttpTrackerTests t = new HttpTrackerTests();
        //    t.FixtureSetup();
        //    t.KeyTest();
        //}
        System.Net.BitTorrent.Tracker.Tracker server;
        System.Net.BitTorrent.Tracker.Listeners.HttpListener listener;
        string prefix ="http://localhost:47124/announce/";
        List<string> keys;

        public HttpTrackerTests(HttpTrackerFixture f)
        {
            keys = f.Keys;
            server = f.Server;
            listener = f.Listener;
        }

        public HttpTrackerTests()
        {
            keys.Clear();
        }


        [Fact]
        public void CanAnnouceOrScrapeTest()
        {
            Tracker.Tracker t = TrackerFactory.Create(new Uri("http://mytracker.com/myurl"));
            Assert.False(t.CanScrape, "#1");
            Assert.True(t.CanAnnounce, "#1b");

            t = TrackerFactory.Create(new Uri("http://mytracker.com/announce/yeah"));
            Assert.False(t.CanScrape, "#2");
            Assert.True(t.CanAnnounce, "#2b");

            t = TrackerFactory.Create(new Uri("http://mytracker.com/announce"));
            Assert.True(t.CanScrape, "#3");
            Assert.True(t.CanAnnounce, "#4");

            HTTPTracker tracker = (HTTPTracker)TrackerFactory.Create(new Uri("http://mytracker.com/announce/yeah/announce"));
            Assert.True(tracker.CanScrape, "#4");
            Assert.True(tracker.CanAnnounce, "#4");
            Assert.Equal("http://mytracker.com/announce/yeah/scrape", tracker.ScrapeUri.ToString());
        }

        [Fact]
        public void AnnounceTest()
        {
            HTTPTracker t = (HTTPTracker)TrackerFactory.Create(new Uri(prefix));
            TrackerConnectionID id = new TrackerConnectionID(t, false, TorrentEvent.Started, new ManualResetEvent(false));
            
            AnnounceResponseEventArgs p = null;
            t.AnnounceComplete += delegate(object o, AnnounceResponseEventArgs e) {
                p = e;
                id.WaitHandle.Set();
            };
            System.Net.BitTorrent.Client.Tracker.AnnounceParameters pars = new AnnounceParameters();
            pars.PeerId = "id";
            pars.InfoHash = new InfoHash (new byte[20]);

            t.Announce(pars, id);
            Wait(id.WaitHandle);
            Assert.NotNull(p);
            Assert.True(p.Successful);
            Assert.Equal(keys[0], t.Key);
        }

        [Fact]
        public void KeyTest()
        {
            System.Net.BitTorrent.Client.Tracker.AnnounceParameters pars = new AnnounceParameters();
            pars.PeerId = "id";
            pars.InfoHash = new InfoHash (new byte[20]);

            Tracker.Tracker t = TrackerFactory.Create(new Uri(prefix + "?key=value"));
            TrackerConnectionID id = new TrackerConnectionID(t, false, TorrentEvent.Started, new ManualResetEvent(false));
            t.AnnounceComplete += delegate { id.WaitHandle.Set(); };
            t.Announce(pars, id);
            Wait(id.WaitHandle);
            Assert.Equal("value", keys[0]);
        }

        [Fact]
        public void ScrapeTest()
        {
            Tracker.Tracker t = TrackerFactory.Create(new Uri(prefix.Substring(0, prefix.Length -1)));
            Assert.True(t.CanScrape, "#1");
            TrackerConnectionID id = new TrackerConnectionID(t, false, TorrentEvent.Started, new ManualResetEvent(false));

            AnnounceResponseEventArgs p = null;
            t.AnnounceComplete += delegate(object o, AnnounceResponseEventArgs e) {
                p = e;
                id.WaitHandle.Set();
            };
            System.Net.BitTorrent.Client.Tracker.AnnounceParameters pars = new AnnounceParameters();
            pars.PeerId = "id";
            pars.InfoHash = new InfoHash(new byte[20]);

            t.Announce(pars, id);
            Wait(id.WaitHandle);
            Assert.NotNull(p);
            Assert.True(p.Successful, "#3");
            Assert.Equal(1, t.Complete);
            Assert.Equal(0, t.Incomplete);
            Assert.Equal(0, t.Downloaded);
        }


        void Wait(WaitHandle handle)
        {
#if NETSTANDARD1_5
            Assert.True(handle.WaitOne(1000000), "Wait handle failed to trigger");
#else
            Assert.True(handle.WaitOne(1000000, true), "Wait handle failed to trigger");
#endif
        }
    }
}
