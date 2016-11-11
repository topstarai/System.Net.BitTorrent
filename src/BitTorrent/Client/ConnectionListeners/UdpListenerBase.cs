#if !DISABLE_DHT
//
// UdpListener.cs
//
// Authors:
//   Alan McGovern <alan.mcgovern@gmail.com>
//
// Copyright (C) 2008 Alan McGovern
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
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Net.BitTorrent.BEncoding;
using System.Net.BitTorrent.Client;
using System.Net.BitTorrent.Common;

namespace System.Net.BitTorrent
{
    public abstract class UdpListenerBase : Listener
    {

        private UdpClient client;

        protected UdpListenerBase(IPEndPoint endpoint)
            :base(endpoint)
        {
            
        }

        protected abstract void OnMessageReceived(byte[] buffer, IPEndPoint endpoint);

        public virtual void Send(byte[] buffer, IPEndPoint endpoint)
        {
            try
            {
               if (endpoint.Address != IPAddress.Any)
                    client.SendAsync(buffer, buffer.Length, endpoint);
            }
            catch(Exception ex)
            {
                Logger.Log (null, "UdpListener could not send message: {0}", ex);
            }
        }

#if IS_CORECLR
        public async override void Start()
#else
        public override void Start()
#endif
        {
            try
            {
                client = new UdpClient(Endpoint);
#if IS_CORECLR
                var receiveTask = client.ReceiveAsync();
                RaiseStatusChanged(ListenerStatus.Listening);
                var result = await receiveTask;
                EndReceive(result);
#else
                client.BeginReceive(EndReceive, null);
                RaiseStatusChanged(ListenerStatus.Listening);
#endif
            }
            catch (SocketException)
            {
                RaiseStatusChanged(ListenerStatus.PortNotFree);
            }
            catch (ObjectDisposedException)
            {
                // Do Nothing
            }
        }

#if IS_CORECLR
        private async void EndReceive(UdpReceiveResult result)
        {
            try
            {
                IPEndPoint e = new IPEndPoint(IPAddress.Any, Endpoint.Port);
                byte[] buffer = null;
                buffer = result.Buffer;
                e = result.RemoteEndPoint;
                OnMessageReceived(buffer, e);
                var r = await client.ReceiveAsync();
                EndReceive(r);
            }
            catch (ObjectDisposedException)
            {
                // Ignore, we're finished!
            }
            catch (SocketException ex)
            {
                // If the destination computer closes the connection
                // we get error code 10054. We need to keep receiving on
                // the socket until we clear all the error states
                if (ex.SocketErrorCode == SocketError.ConnectionReset)
                {
                    while (true)
                    {
                        try
                        {
                            var r = await client.ReceiveAsync();
                            EndReceive(r);
                            return;
                        }
                        catch (ObjectDisposedException)
                        {
                            return;
                        }
                        catch (SocketException e)
                        {
                            if (e.SocketErrorCode != SocketError.ConnectionReset)
                                return;
                        }
                    }
                }
            }
        }
#else
        private void EndReceive(IAsyncResult result)
        {
            try
            {
                IPEndPoint e = new IPEndPoint(IPAddress.Any, Endpoint.Port);
                byte[] buffer = client.EndReceive(result, ref e);

                OnMessageReceived(buffer, e);
                client.BeginReceive(EndReceive, null);
            }
            catch (ObjectDisposedException)
            {
                // Ignore, we're finished!
            }
            catch (SocketException ex)
            {
                // If the destination computer closes the connection
                // we get error code 10054. We need to keep receiving on
                // the socket until we clear all the error states
                if (ex.ErrorCode == 10054)
                {
                    while (true)
                    {
                        try
                        {
                            client.BeginReceive(EndReceive, null);
                            return;
                        }
                        catch (ObjectDisposedException)
                        {
                            return;
                        }
                        catch (SocketException e)
                        {
                            if (e.ErrorCode != 10054)
                                return;
                        }
                    }
                }
            }
        }
#endif
        public override void Stop()
        {
            try
            {
#if IS_CORECLR
                client.Dispose();
#else
                client.Close();
#endif
            }
            catch
            {
                // FIXME: Not needed
            }
        }
    }
}
#endif