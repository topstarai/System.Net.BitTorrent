#if !DISABLE_DHT
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using System.Net.BitTorrent.Dht.Messages;
using System.Net.BitTorrent.BEncoding;
using System.Net.BitTorrent.Common;

namespace System.Net.BitTorrent.Dht
{
    
    public class MessageTests:IDisposable
    {
        //static void Main(string[] args)
        //{
        //    MessageTests t = new MessageTests();
        //    t.GetPeersResponseEncode();
        //}
        private NodeId id = new NodeId(Encoding.UTF8.GetBytes("abcdefghij0123456789"));
        private NodeId infohash = new NodeId(Encoding.UTF8.GetBytes("mnopqrstuvwxyz123456"));
        private BEncodedString token = "aoeusnth";
        private BEncodedString transactionId = "aa";

        private QueryMessage message;

        public MessageTests()
        {
            Message.UseVersionKey = false;
        }

        #region Encode Tests

        [Fact]
        public void AnnouncePeerEncode()
        {
            Node n = new System.Net.BitTorrent.Dht.Node(NodeId.Create(), null);
            n.Token = token;
            AnnouncePeer m = new AnnouncePeer(id, infohash, 6881, token);
            m.TransactionId = transactionId;

            Compare(m, "d1:ad2:id20:abcdefghij01234567899:info_hash20:mnopqrstuvwxyz1234564:porti6881e5:token8:aoeusnthe1:q13:announce_peer1:t2:aa1:y1:qe");
        }

        [Fact]
        public void AnnouncePeerResponseEncode()
        {
            AnnouncePeerResponse m = new AnnouncePeerResponse(infohash, transactionId);

            Compare(m, "d1:rd2:id20:mnopqrstuvwxyz123456e1:t2:aa1:y1:re");
        }

        [Fact]
        public void FindNodeEncode()
        {
            FindNode m = new FindNode(id, infohash);
            m.TransactionId = transactionId;

            Compare(m, "d1:ad2:id20:abcdefghij01234567896:target20:mnopqrstuvwxyz123456e1:q9:find_node1:t2:aa1:y1:qe");
            message = m;
        }

        [Fact]
        public void FindNodeResponseEncode()
        {
            FindNodeResponse m = new FindNodeResponse(id, transactionId);
            m.Nodes = "def456...";

            Compare(m, "d1:rd2:id20:abcdefghij01234567895:nodes9:def456...e1:t2:aa1:y1:re");
        }

        [Fact]
        public void GetPeersEncode()
        {
            GetPeers m = new GetPeers(id, infohash);
            m.TransactionId = transactionId;

            Compare(m, "d1:ad2:id20:abcdefghij01234567899:info_hash20:mnopqrstuvwxyz123456e1:q9:get_peers1:t2:aa1:y1:qe");
            message = m;
        }

        [Fact]
        public void GetPeersResponseEncode()
        {
            GetPeersResponse m = new GetPeersResponse(id, transactionId, token);
            m.Values = new BEncodedList();
            m.Values.Add((BEncodedString)"axje.u");
            m.Values.Add((BEncodedString)"idhtnm");
            Compare(m, "d1:rd2:id20:abcdefghij01234567895:token8:aoeusnth6:valuesl6:axje.u6:idhtnmee1:t2:aa1:y1:re");
        }

        [Fact]
        public void PingEncode()
        {
            Ping m = new Ping(id);
            m.TransactionId = transactionId;
            
            Compare(m, "d1:ad2:id20:abcdefghij0123456789e1:q4:ping1:t2:aa1:y1:qe");
            message = m;
        }

        [Fact]
        public void PingResponseEncode()
        {
            PingResponse m = new PingResponse(infohash, transactionId);

            Compare(m, "d1:rd2:id20:mnopqrstuvwxyz123456e1:t2:aa1:y1:re");
        }


        #endregion

        #region Decode Tests

        [Fact]
        public void AnnouncePeerDecode()
        {
            string text = "d1:ad2:id20:abcdefghij01234567899:info_hash20:mnopqrstuvwxyz1234564:porti6881e5:token8:aoeusnthe1:q13:announce_peer1:t2:aa1:y1:qe";
            AnnouncePeer m = (AnnouncePeer)Decode("d1:ad2:id20:abcdefghij01234567899:info_hash20:mnopqrstuvwxyz1234564:porti6881e5:token8:aoeusnthe1:q13:announce_peer1:t2:aa1:y1:qe");
            Assert.Equal(m.TransactionId, transactionId);
            Assert.Equal(m.MessageType, QueryMessage.QueryType);
            Assert.Equal(id, m.Id);
            Assert.Equal(infohash, m.InfoHash);
            Assert.Equal((BEncodedNumber)6881, m.Port);
            Assert.Equal(token, m.Token);

            Compare(m, text);
            message = m;
        }


        [Fact]
        public void AnnouncePeerResponseDecode()
        {
            // Register the query as being sent so we can decode the response
            AnnouncePeerDecode();
            MessageFactory.RegisterSend(message);
            string text = "d1:rd2:id20:mnopqrstuvwxyz123456e1:t2:aa1:y1:re";

            AnnouncePeerResponse m = (AnnouncePeerResponse)Decode(text);
            Assert.Equal(infohash, m.Id);

            Compare(m, text);
        }

        [Fact]
        public void FindNodeDecode()
        {
            string text ="d1:ad2:id20:abcdefghij01234567896:target20:mnopqrstuvwxyz123456e1:q9:find_node1:t2:aa1:y1:qe";
            FindNode m = (FindNode)Decode(text);

            Assert.Equal(id, m.Id);
            Assert.Equal(infohash, m.Target);
            Compare(m, text);
        }

        [Fact]
        public void FindNodeResponseDecode()
        {
            FindNodeEncode();
            MessageFactory.RegisterSend(message);
            string text = "d1:rd2:id20:abcdefghij01234567895:nodes9:def456...e1:t2:aa1:y1:re";
            FindNodeResponse m = (FindNodeResponse)Decode(text);

            Assert.Equal(id, m.Id);
            Assert.Equal((BEncodedString)"def456...", m.Nodes);
            Assert.Equal(transactionId, m.TransactionId);

            Compare(m, text);
        }

        [Fact]
        public void GetPeersDecode()
        {
            string text = "d1:ad2:id20:abcdefghij01234567899:info_hash20:mnopqrstuvwxyz123456e1:q9:get_peers1:t2:aa1:y1:qe";
            GetPeers m = (GetPeers)Decode(text);

            Assert.Equal(infohash, m.InfoHash);
            Assert.Equal(id, m.Id);
            Assert.Equal(transactionId, m.TransactionId);

            Compare(m, text);
        }

        [Fact]
        public void GetPeersResponseDecode()
        {
            GetPeersEncode();
            MessageFactory.RegisterSend(message);

            string text = "d1:rd2:id20:abcdefghij01234567895:token8:aoeusnth6:valuesl6:axje.u6:idhtnmee1:t2:aa1:y1:re";
            GetPeersResponse m = (GetPeersResponse)Decode(text);

            Assert.Equal(token, m.Token);
            Assert.Equal(id, m.Id);

            BEncodedList l = new BEncodedList();
            l.Add((BEncodedString)"axje.u");
            l.Add((BEncodedString)"idhtnm");
            Assert.Equal(l, m.Values);

            Compare(m, text);
        }

        [Fact]
        public void PingDecode()
        {
            string text = "d1:ad2:id20:abcdefghij0123456789e1:q4:ping1:t2:aa1:y1:qe";
            Ping m = (Ping) Decode(text);

            Assert.Equal(id, m.Id);

            Compare(m, text);
        }

        [Fact]
        public void PingResponseDecode()
        {
            PingEncode();
            MessageFactory.RegisterSend(message);

            string text = "d1:rd2:id20:mnopqrstuvwxyz123456e1:t2:aa1:y1:re";
            PingResponse m = (PingResponse)Decode(text);

            Assert.Equal(infohash, m.Id);

            Compare(m, "d1:rd2:id20:mnopqrstuvwxyz123456e1:t2:aa1:y1:re");
        }

        #endregion


        private void Compare(Message m, string expected)
        {
            byte[] b = m.Encode();
            Assert.Equal(Encoding.UTF8.GetString(b), expected);
        }

        private Message Decode(string p)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(p);
            return MessageFactory.DecodeMessage(BEncodedValue.Decode<BEncodedDictionary>(buffer));
        }

        public void Dispose()
        {
            Message.UseVersionKey = true;
        }
    }
}
#endif