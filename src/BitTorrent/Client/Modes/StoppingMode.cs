﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Net.BitTorrent.Common;

namespace System.Net.BitTorrent.Client
{
    class StoppingMode : Mode
    {
        WaitHandleGroup handle = new WaitHandleGroup();

        public override TorrentState State
        {
            get { return TorrentState.Stopping; }
        }

        public StoppingMode(TorrentManager manager)
            : base(manager)
        {
            CanAcceptConnections = false;
            ClientEngine engine = manager.Engine;
            if (manager.Mode is HashingMode)
                handle.AddHandle(((HashingMode)manager.Mode).hashingWaitHandle, "Hashing");

            if (manager.TrackerManager.CurrentTracker != null && manager.TrackerManager.CurrentTracker.Status == TrackerState.Ok)
                handle.AddHandle(manager.TrackerManager.Announce(TorrentEvent.Stopped), "Announcing");

            foreach (PeerId id in manager.Peers.ConnectedPeers)
                if (id.Connection != null)
                    id.Connection.Dispose();

            manager.Peers.ClearAll();

            handle.AddHandle(engine.DiskManager.CloseFileStreams(manager), "DiskManager");

            manager.Monitor.Reset();
            manager.PieceManager.Reset();
            engine.ConnectionManager.CancelPendingConnects(manager);
            engine.Stop();
        }

        public override void HandlePeerConnected(PeerId id, System.Net.BitTorrent.Common.Direction direction)
        {
            id.CloseConnection();
        }

        public override void Tick(int counter)
        {
#if NETSTANDARD1_5
            if (handle.WaitOne(0))
            {
                handle.Dispose();
                Manager.Mode = new StoppedMode(Manager);
            }
#else
            if (handle.WaitOne(0, true))
            {
                handle.Close();
                Manager.Mode = new StoppedMode(Manager);
            }
#endif
        }
    }
}
