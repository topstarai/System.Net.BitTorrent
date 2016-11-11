//
// LocalPeerListener.cs
//
// Authors:
//   Jared Hendry hendry.jared@gmail.com
//
// Copyright (C) 2008 Jared Hendry
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Net.BitTorrent.Common;
using System.Net.BitTorrent.Client.Encryption;

namespace System.Net.BitTorrent.Client
{
    class LocalPeerListener : Listener
    {
        const int MulticastPort = 6771;
        static readonly IPAddress multicastIpAddress = IPAddress.Parse("239.192.152.143");

        private ClientEngine engine;
        private UdpClient udpClient;

        public LocalPeerListener(ClientEngine engine)
            : base(new IPEndPoint(IPAddress.Any, 6771))
        {
            this.engine = engine;
        }

#if IS_CORECLR
        public async override void Start()
#else
        public override void Start()
#endif
        {
            if (Status == ListenerStatus.Listening)
                return;
            try
            {
                udpClient = new UdpClient(MulticastPort);
                udpClient.JoinMulticastGroup(multicastIpAddress);
#if IS_CORECLR
                var receive = await udpClient.ReceiveAsync();
                OnReceiveCallBack(receive, udpClient);
#else
                udpClient.BeginReceive(OnReceiveCallBack, udpClient);
#endif
                RaiseStatusChanged(ListenerStatus.Listening);
            }
            catch
            {
                RaiseStatusChanged(ListenerStatus.PortNotFree);
            }
        }

        public override void Stop()
        {
            if (Status == ListenerStatus.NotListening)
                return;

            RaiseStatusChanged(ListenerStatus.NotListening);
            UdpClient c = udpClient;
            udpClient = null;
            if (c != null)
#if IS_CORECLR
                c.Dispose();
#else
            c.Close();
#endif
        }
#if IS_CORECLR
        private async void OnReceiveCallBack(UdpReceiveResult ar, UdpClient client)
        {
            UdpClient u = client;
            IPEndPoint e = ar.RemoteEndPoint;//new IPEndPoint(IPAddress.Any, 0);
            try
            {
                byte[] receiveBytes = ar.Buffer;// u.EndReceive(ar, ref e);
                string receiveString = Encoding.ASCII.GetString(receiveBytes);

                Regex exp = new Regex("BT-SEARCH \\* HTTP/1.1\\r\\nHost: 239.192.152.143:6771\\r\\nPort: (?<port>[^@]+)\\r\\nInfohash: (?<hash>[^@]+)\\r\\n\\r\\n\\r\\n");
                Match match = exp.Match(receiveString);

                if (!match.Success)
                    return;

                int portcheck = Convert.ToInt32(match.Groups["port"].Value);
                if (portcheck < 0 || portcheck > 65535)
                    return;

                TorrentManager manager = null;
                InfoHash matchHash = InfoHash.FromHex(match.Groups["hash"].Value);
                for (int i = 0; manager == null && i < engine.Torrents.Count; i++)
                    if (engine.Torrents[i].InfoHash == matchHash)
                        manager = engine.Torrents[i];

                if (manager == null)
                    return;

                Uri uri = new Uri("tcp://" + e.Address.ToString() + ':' + match.Groups["port"].Value);
                Peer peer = new Peer("", uri, EncryptionTypes.All);

                // Add new peer to matched Torrent
                if (!manager.HasMetadata || !manager.Torrent.IsPrivate)
                {
                    ClientEngine.MainLoop.Queue(delegate {
                        int count = manager.AddPeersCore(peer);
                        manager.RaisePeersFound(new LocalPeersAdded(manager, count, 1));
                    });
                }
            }
            catch
            {
                // Failed to receive data, ignore
            }
            finally
            {
                try
                {
                    var r = await u.ReceiveAsync();
                    OnReceiveCallBack(r, u);
                    //u.BeginReceive(OnReceiveCallBack, ar.AsyncState);
                }
                catch
                {
                    // It's closed
                }
            }
        }
#else
        private void OnReceiveCallBack(IAsyncResult ar)
        {
            UdpClient u = (UdpClient)ar.AsyncState;
            IPEndPoint e = new IPEndPoint(IPAddress.Any, 0);
            try
            {
                byte[] receiveBytes = u.EndReceive(ar, ref e);
                string receiveString = Encoding.ASCII.GetString(receiveBytes);

                Regex exp = new Regex("BT-SEARCH \\* HTTP/1.1\\r\\nHost: 239.192.152.143:6771\\r\\nPort: (?<port>[^@]+)\\r\\nInfohash: (?<hash>[^@]+)\\r\\n\\r\\n\\r\\n");
                Match match = exp.Match(receiveString);

                if (!match.Success)
                    return;

                int portcheck = Convert.ToInt32(match.Groups["port"].Value);
                if (portcheck < 0 || portcheck > 65535)
                    return;

                TorrentManager manager = null;
                InfoHash matchHash = InfoHash.FromHex(match.Groups["hash"].Value);
                for (int i = 0; manager == null && i < engine.Torrents.Count; i ++)
                    if (engine.Torrents [i].InfoHash == matchHash)
                        manager = engine.Torrents [i];
                
                if (manager == null)
                    return;

                Uri uri = new Uri("tcp://" + e.Address.ToString() + ':' + match.Groups["port"].Value);
                Peer peer = new Peer("", uri, EncryptionTypes.All);

                // Add new peer to matched Torrent
                if (!manager.HasMetadata || !manager.Torrent.IsPrivate)
                {
                    ClientEngine.MainLoop.Queue(delegate {
                        int count = manager.AddPeersCore (peer);
                        manager.RaisePeersFound(new LocalPeersAdded(manager, count, 1));
                    });
                }
            }
            catch
            {
                // Failed to receive data, ignore
            }
            finally
            {
                try
                {
                    u.BeginReceive(OnReceiveCallBack, ar.AsyncState);
                }
                catch
                {
                    // It's closed
                }
            }
        }

#endif

    }
}
