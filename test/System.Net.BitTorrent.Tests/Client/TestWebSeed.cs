using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using System.Net.BitTorrent.Client.Connections;
using System.Net;
using System.Net.BitTorrent.Client.Messages.Standard;
using System.Net.BitTorrent.Client;
using System.Threading;
using System.Net.BitTorrent.Client.Messages;
using System.Text.RegularExpressions;
using System.Net.BitTorrent.Common;
using System.Net.Http;

namespace System.Net.BitTorrent.Client
{
    
    public class TestWebSeed : IDisposable
    {
        Regex rangeMatcher = new Regex(@"(\d{1,10})-(\d{1,10})");
        //static void Main(string[] args)
        //{
        //    TestWebSeed s = new TestWebSeed();
        //    for (int i = 0; i < 50; i++)
        //    {
        //        s.Setup();
        //        s.SingleFileTorrent();
        //        s.TearDown();
        //    }
        //}

        bool partialData;
        public readonly int Count = 5;
        TestRig rig;
        HttpConnection connection;
        HttpListener listener;
        //private RequestMessage m;
        private string listenerURL = "http://127.0.0.1:120/announce/";
        int amountSent;

        PeerId id;
        MessageBundle requests;
        int numberOfPieces = 50;

        public TestWebSeed()
        {
            requestedUrl.Clear();
            partialData = false;
            int i;
            for (i = 0; i < 1000; i++)
            {
                try
                {
#if IS_CORECLR
                    var uri = new Uri(listenerURL);
                    listener = new HttpListener(IPAddress.Parse(uri.Host),uri.Port);
#else
                    listener = new HttpListener();
                    listener.Prefixes.Add(string.Format(listenerURL, i));
#endif
                    listener.Start();
                    break;
                }
                catch
                {

                }
            }
#if IS_CORECLR
            listener.BeginGetContext(GotContext, null);
#else
            listener.BeginGetContext(GotContext, null);
#endif

            rig = TestRig.CreateMultiFile();
            connection = new HttpConnection(new Uri(string.Format(listenerURL, i)));
            connection.Manager = rig.Manager;

            id = new PeerId(new Peer("this is my id", connection.Uri), rig.Manager);
            id.Connection = connection;
            id.IsChoking = false;
            id.AmInterested = true;
            id.BitField.SetAll(true);
            id.MaxPendingRequests = numberOfPieces;
            
            requests = rig.Manager.PieceManager.Picker.PickPiece(id, new List<PeerId>(), numberOfPieces);
        }

        public void Dispose()
        {
            listener.Close();
            rig.Dispose();
        }

        [Fact]
        public void TestPartialData()
        {
            Assert.Throws<WebException>(() => {
                partialData = true;
                RecieveFirst();
            });
        }

        [Fact]
        public void TestInactiveServer()
        {
            Assert.Throws<WebException>(() => {
                connection.ConnectionTimeout = TimeSpan.FromMilliseconds(100);
#if IS_CORECLR
                listener.Dispose();
#else
                listener.Stop();
#endif
                RecieveFirst();
            });
        }

        [Fact]
        public void RecieveFirst()
        {
            byte[] buffer = new byte[1024 * 1024 * 3];
            IAsyncResult receiveResult = connection.BeginReceive(buffer, 0, 4, null, null);
            IAsyncResult sendResult = connection.BeginSend(requests.Encode(), 0, requests.ByteLength, null, null);
            amountSent = requests.ByteLength;

            CompleteSendOrReceiveFirst(buffer, receiveResult, sendResult);
        }

        [Fact]
        public void SendFirst()
        {
            byte[] buffer = new byte[1024 * 1024 * 3];
            IAsyncResult sendResult = connection.BeginSend(requests.Encode(), 0, requests.ByteLength, null, null);
            IAsyncResult receiveResult = connection.BeginReceive(buffer, 0, 4, null, null);
            amountSent = requests.ByteLength;

            CompleteSendOrReceiveFirst(buffer, receiveResult, sendResult);
        }

        [Fact]
        public void ChunkedRequest()
        {
            if (requests.Messages.Count != 0)
                rig.Manager.PieceManager.Picker.CancelRequests(id);
            
            requests = rig.Manager.PieceManager.Picker.PickPiece(id, new List<PeerId>(), 256);

            byte[] sendBuffer = requests.Encode();
            int offset = 0;
            amountSent = Math.Min(sendBuffer.Length - offset, 2048);
            IAsyncResult sendResult = connection.BeginSend(sendBuffer, offset, amountSent, null, null);
#if IS_CORECLR
            while (sendResult.AsyncWaitHandle.WaitOne(10))
#else
            while (sendResult.AsyncWaitHandle.WaitOne(10, true))
#endif
            {
                Assert.Equal(amountSent, connection.EndSend(sendResult));
                offset += amountSent;
                amountSent = Math.Min(sendBuffer.Length - offset, 2048);
                if (amountSent == 0)
                    Assert.True(false,"This should never happen");
                sendResult = connection.BeginSend(sendBuffer, offset, amountSent, null, null);
            }

            byte[] buffer = new byte[1024 * 1024 * 3];
            IAsyncResult receiveResult = connection.BeginReceive(buffer, 0, 4, null, null);

            CompleteSendOrReceiveFirst(buffer, receiveResult, sendResult);
        }

        [Fact]
        public void MultipleChunkedRequests()
        {
            ChunkedRequest();
            ChunkedRequest();
            ChunkedRequest();
        }

        private void CompleteSendOrReceiveFirst(byte[] buffer, IAsyncResult receiveResult, IAsyncResult sendResult)
        {
            int received = 0;
            Wait(receiveResult.AsyncWaitHandle);
            while ((received = connection.EndReceive(receiveResult)) != 0)
            {
                if (received != 4)
                    throw new Exception("Should be 4 bytes");

                int size = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, 0));
                received = 0;

                while (received != size)
                {
                    IAsyncResult r = connection.BeginReceive(buffer, received + 4, size - received, null, null);
                    Wait(r.AsyncWaitHandle);
                    received += connection.EndReceive(r);
                }
                PieceMessage m = (PieceMessage)PeerMessage.DecodeMessage(buffer, 0, size + 4, rig.Manager);
                RequestMessage request = (RequestMessage)requests.Messages[0];
                Assert.Equal(request.PieceIndex, m.PieceIndex);
                Assert.Equal(request.RequestLength, m.RequestLength);
                Assert.Equal(request.StartOffset, m.StartOffset);

                for (int i = 0; i < request.RequestLength; i++)
                    if (buffer[i + 13] != (byte)(m.PieceIndex * rig.Torrent.PieceLength + m.StartOffset + i))
                        throw new Exception("Corrupted data received");
                
                requests.Messages.RemoveAt(0);

                if (requests.Messages.Count == 0)
                {
                    //receiveResult = connection.BeginReceive(buffer, 0, 4, null, null);
                    Wait(sendResult.AsyncWaitHandle);
                    Assert.Equal(connection.EndSend(sendResult), amountSent);
                    break;
                }
                else
                {
                    receiveResult = connection.BeginReceive(buffer, 0, 4, null, null);
                }
            }

            Uri baseUri = new Uri(this.listenerURL);
            baseUri = new Uri(baseUri, rig.Manager.Torrent.Name + "/");
            if (rig.Manager.Torrent.Files.Length > 1)
            {
                Assert.Equal(new Uri(baseUri, rig.Manager.Torrent.Files[0].Path).AbsolutePath, requestedUrl[0]);
                Assert.Equal(new Uri(baseUri, rig.Manager.Torrent.Files[1].Path).AbsolutePath, requestedUrl[1]);
            }
        }

        private List<string> requestedUrl = new List<string>();
        private void GotContext(IAsyncResult result)
        {
            try
            {
                HttpListenerContext c = listener.EndGetContext(result);
                Console.WriteLine("Got Context");
#if IS_CORECLR
                requestedUrl.Add(c.Request.RequestUri.OriginalString);
#else
                requestedUrl.Add(c.Request.Url.OriginalString);
#endif
                Match match = null;
                string range = c.Request.Headers["range"];

                if (!(range != null && (match = rangeMatcher.Match(range)) != null))
                    Assert.True(false,"No valid range specified");

                int start = int.Parse(match.Groups[1].Captures[0].Value);
                int end = int.Parse(match.Groups[2].Captures[0].Value);


                long globalStart = 0;
                bool exists = false;
                string p;
                if(rig.Manager.Torrent.Files.Length > 1)
#if IS_CORECLR
                    p = c.Request.RequestUri.AbsoluteUri.Substring(10 + rig.Torrent.Name.Length + 1);
#else
                    p = c.Request.RawUrl.Substring(10 + rig.Torrent.Name.Length + 1);
#endif
                else
#if IS_CORECLR
                    p = c.Request.RequestUri.AbsoluteUri.Substring(10);
#else
                    p = c.Request.RawUrl.Substring(10);
#endif
                foreach (TorrentFile file in rig.Manager.Torrent.Files)
                {
                    if (file.Path.Replace('\\', '/') != p)
                    {
                        globalStart += file.Length;
                        continue;
                    }
                    globalStart += start;
                    exists = start < file.Length && end < file.Length;
                    break;
                }

                TorrentFile[] files = rig.Manager.Torrent.Files;
#if IS_CORECLR
                if (files.Length == 1 && rig.Torrent.GetRightHttpSeeds[0] == c.Request.RequestUri.OriginalString)
#else
                if (files.Length == 1 && rig.Torrent.GetRightHttpSeeds[0] == c.Request.Url.OriginalString)
#endif
                {
                    globalStart = 0;
                    exists = start < files[0].Length && end < files[0].Length;
                }

                if (!exists)
                {
                    c.Response.StatusCode = (int) HttpStatusCode.RequestedRangeNotSatisfiable;
                    c.Response.Close();
                }
                else
                {
                    byte[] data = partialData ? new byte[(end - start) / 2] : new byte[end - start + 1];
                    for (int i = 0; i < data.Length; i++)
                        data[i] = (byte)(globalStart + i);
#if IS_CORECLR
                    c.Response.Dispose();
#else
                    c.Response.Close(data, false);
#endif
                }

                listener.BeginGetContext(GotContext, null);
            }
            catch
            {
            }
        }

        [Fact]
        public void SingleFileTorrent()
        {
            rig.Dispose();
            rig = TestRig.CreateSingleFile();
            string url = rig.Torrent.GetRightHttpSeeds[0];
            connection = new HttpConnection(new Uri (url));
            connection.Manager = rig.Manager;

            id = new PeerId(new Peer("this is my id", connection.Uri), rig.Manager);
            id.Connection = connection;
            id.IsChoking = false;
            id.AmInterested = true;
            id.BitField.SetAll(true);
            id.MaxPendingRequests = numberOfPieces;

            requests = rig.Manager.PieceManager.Picker.PickPiece(id, new List<PeerId>(), numberOfPieces);
            RecieveFirst();
            Assert.Equal(url, requestedUrl[0]);
        }

        void Wait(WaitHandle handle)
        {
#if IS_CORECLR
            Assert.True(handle.WaitOne(5000), "WaitHandle did not trigger");
#else
            Assert.True(handle.WaitOne(5000, true), "WaitHandle did not trigger");
#endif
        }
    }
}
