using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using System.Net.BitTorrent.Client.Tracker;
using System.Net.BitTorrent.Client;
using System.Threading;
using System.Net.BitTorrent.Common;

namespace System.Net.BitTorrent.Tracker
{
    [TestFixture]
    public class TrackerTests
    {
        //static void Main(string[] args)
        //{
        //    TrackerTests t = new TrackerTests();
        //    t.FixtureSetup();
        //    t.Setup();
        //    t.MultipleAnnounce();
        //    t.FixtureTeardown();
        //}
        Uri uri = new Uri("http://127.0.0.1:23456/");
        System.Net.BitTorrent.Tracker.Listeners.HttpListener listener;
        System.Net.BitTorrent.Tracker.Tracker server;
        //System.Net.BitTorrent.Client.Tracker.HTTPTracker tracker;
        [TestFixtureSetUp]
        public void FixtureSetup()
        {
            listener = new System.Net.BitTorrent.Tracker.Listeners.HttpListener(uri.OriginalString);
            listener.Start();
            server = new System.Net.BitTorrent.Tracker.Tracker();
            server.RegisterListener(listener);
            listener.Start();
        }

        [TestFixtureTearDown]
        public void FixtureTeardown()
        {
            listener.Stop();
            server.Dispose();
        }

        [SetUp]
        public void Setup()
        {
            //tracker = new System.Net.BitTorrent.Client.Tracker.HTTPTracker(uri);
        }

        [Test]
        public void MultipleAnnounce()
        {
            int announceCount = 0;
            Random r = new Random();
            ManualResetEvent handle = new ManualResetEvent(false);

            for (int i=0; i < 20; i++)
            {
                InfoHash infoHash = new InfoHash(new byte[20]);
                r.NextBytes(infoHash.Hash);
                TrackerTier tier = new TrackerTier(new string[] { uri.ToString() });
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

            Assert.IsTrue(handle.WaitOne(5000, true), "Some of the responses weren't received");
        }
    }
}
