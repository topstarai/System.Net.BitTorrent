using System;
using System.Collections.Generic;
using System.Text;
using System.Net.BitTorrent.Dht;
using System.Net.BitTorrent.Dht.Listeners;
using System.Net;
using System.IO;
using System.Net.BitTorrent.Common;
using System.Net.BitTorrent.Tracker.Listeners;
using System.Net.BitTorrent;

namespace DhtSampleClient
{
    class Program
    {
        static void Main(string[] args)
        {
            DhtListener listener = new DhtListener(new IPEndPoint(IPAddress.Parse("192.168.0.6"), 15000));
            DhtEngine engine = new DhtEngine(listener);

            byte[] nodes = null;
            if (File.Exists("mynodes"))
                nodes = File.ReadAllBytes("mynodes");

            listener.Start();
            engine.PeersFound += delegate (object o, PeersFoundEventArgs e)
            {
                Console.WriteLine("I FOUND PEERS: {0}", e.Peers.Count);
                engine.Start(nodes);

                Random random = new Random(5);
                byte[] b = new byte[20];
                lock (random)
                    random.NextBytes(b);

                while (Console.ReadLine() != "q")
                {
                    for (int i = 0; i < 30; i++)
                    {
                        Console.WriteLine("Waiting: {0} seconds left", (30 - i));
                        System.Threading.Thread.Sleep(1000);
                    }
                    // Get some peers for the torrent
                    engine.GetPeers(new InfoHash(b));
                    random.NextBytes(b);
                }
                File.WriteAllBytes("mynodes", engine.SaveNodes());
            };
        }
    }
}