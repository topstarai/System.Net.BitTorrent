 //
// ConnectionListener.cs
//
// Authors:
//   Alan McGovern alan.mcgovern@gmail.com
//
// Copyright (C) 2006 Alan McGovern
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
using System.Net.BitTorrent.Client.Encryption;
using System.Net.BitTorrent.Common;
using System.Net.BitTorrent.Client.Connections;

namespace System.Net.BitTorrent.Client
{
    /// <summary>
    /// Accepts incoming connections and passes them off to the right TorrentManager
    /// </summary>
    public class SocketListener : PeerListener
    {
#if IS_CORECLR
#else
        private AsyncCallback endAcceptCallback;
#endif
        private Socket listener;

        public SocketListener(IPEndPoint endpoint)
            : base(endpoint)
        {
#if IS_CORECLR
#else
            this.endAcceptCallback = EndAccept;
#endif
        }

#if IS_CORECLR
        private async void EndAccept(Socket result, Socket listenerSocket)
#else
        private void EndAccept(IAsyncResult result)
#endif
        {
            Socket peerSocket = null;
            try
            {
#if IS_CORECLR
                Socket listener = listenerSocket;//(Socket)result.AsyncState;
                peerSocket = result;//listener.EndAccept(result);
#else
                Socket listener = (Socket)result.AsyncState;
                peerSocket =listener.EndAccept(result);
#endif

                IPEndPoint endpoint = (IPEndPoint)peerSocket.RemoteEndPoint;
                Uri uri = new Uri("tcp://" + endpoint.Address.ToString() + ':' + endpoint.Port);
                Peer peer = new Peer("", uri, EncryptionTypes.All);
                IConnection connection = null;
                if (peerSocket.AddressFamily == AddressFamily.InterNetwork)
                    connection = new IPV4Connection(peerSocket, true);
                else
                    connection = new IPV6Connection(peerSocket, true);


                RaiseConnectionReceived(peer, connection, null);
            }
            catch (SocketException)
            {
                // Just dump the connection
                if (peerSocket != null)
#if IS_CORECLR
                    peerSocket.Dispose();
#else
                    peerSocket.Close();
#endif
            }
            catch (ObjectDisposedException)
            {
                // We've stopped listening
            }
            finally
            {
                try
                {
                    if (Status == ListenerStatus.Listening)
                    {
#if IS_CORECLR
                        var r = await listener.AcceptAsync();
                        EndAccept(r, listener);
#else
                        listener.BeginAccept(endAcceptCallback, listener);
#endif
                    }
                }
                catch (ObjectDisposedException)
                {

                }
            }
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
                listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(Endpoint);
                listener.Listen(6);
#if IS_CORECLR
                var acceptTask = listener.AcceptAsync();
                RaiseStatusChanged(ListenerStatus.Listening);
                var result = await acceptTask;
                EndAccept(result, listener);
#else
                listener.BeginAccept(endAcceptCallback, listener);
                RaiseStatusChanged(ListenerStatus.Listening);
#endif
            }
            catch (SocketException)
            {
                RaiseStatusChanged(ListenerStatus.PortNotFree);
            }
        }

        public override void Stop()
        {
            RaiseStatusChanged(ListenerStatus.NotListening);

            if (listener != null)
#if IS_CORECLR
                listener.Dispose();
#else
                listener.Close();
#endif
        }
    }
}