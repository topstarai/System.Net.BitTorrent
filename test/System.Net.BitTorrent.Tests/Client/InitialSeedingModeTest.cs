using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net.BitTorrent.Client.Messages.FastPeer;
using System.Net.BitTorrent.Client.Messages.Standard;
using System.Net.BitTorrent.Common;
using Xunit;

namespace System.Net.BitTorrent.Client
{
    
    public class InitialSeedingModeTest:IDisposable
    {
        InitialSeedingMode Mode {
            get { return Rig.Manager.Mode as InitialSeedingMode; }
        }

        TestRig Rig {
            get; set;
        }

        public InitialSeedingModeTest()
        {
            Rig = TestRig.CreateSingleFile(Piece.BlockSize * 20, Piece.BlockSize * 2);
            Rig.Manager.Bitfield.Not ();
            Rig.Manager.Mode = new InitialSeedingMode(Rig.Manager);
        }

        [Fact]
        public void SwitchingModesSendsHaves()
        {
            Rig.Manager.Peers.ConnectedPeers.Add(Rig.CreatePeer(true, true));
            Rig.Manager.Peers.ConnectedPeers.Add(Rig.CreatePeer(true, false));

            var peer = Rig.CreatePeer(true);
            peer.BitField.SetAll(true);
            Mode.HandlePeerConnected(peer, Direction.Incoming);
            Mode.Tick(0);

            Assert.True(Rig.Manager.Peers.ConnectedPeers[0].Dequeue() is HaveAllMessage, "#1");
            BitfieldMessage m = (BitfieldMessage) Rig.Manager.Peers.ConnectedPeers[1].Dequeue();
            Assert.True(m.BitField.AllTrue, "#2");
        }

        public void Dispose()
        {
            Rig.Dispose();
        }
    }
}
