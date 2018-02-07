using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using System.Net.BitTorrent.Client;
using System.Net;
using System.Net.Sockets;

namespace System.Net.BitTorrent.Client
{
    
    public class ConnectionListenerTests:IDisposable
    {
        //static void Main(string[] args)
        //{
        //    ConnectionListenerTests t = new ConnectionListenerTests();
        //    t.Setup();
        //    t.AcceptThree();
        //    t.Teardown();
        //}
        private SocketListener listener;
        private IPEndPoint endpoint;
        public ConnectionListenerTests()
        {
            endpoint = new IPEndPoint(IPAddress.Loopback, 55652);
            listener = new SocketListener(endpoint);
            listener.Start();
            System.Threading.Thread.Sleep(100);
        }

        [Fact]
        public void AcceptThree()
        {
            using (TcpClient c = new TcpClient(AddressFamily.InterNetwork))
#if NETSTANDARD1_5
                c.ConnectAsync(endpoint.Address,endpoint.Port);
#else
            c.Connect(endpoint);
#endif
            using (TcpClient c = new TcpClient(AddressFamily.InterNetwork))
#if NETSTANDARD1_5
                c.ConnectAsync(endpoint.Address, endpoint.Port);
#else
            c.Connect(endpoint);
#endif
            using (TcpClient c = new TcpClient(AddressFamily.InterNetwork))
#if NETSTANDARD1_5
            c.ConnectAsync(endpoint.Address, endpoint.Port);
#else
            c.Connect(endpoint);
#endif
        }

        [Fact]
        public void ChangePortThree()
        {
            endpoint.Port++;
            listener.ChangeEndpoint(endpoint);
            AcceptThree();

            endpoint.Port++;
            listener.ChangeEndpoint(endpoint);
            AcceptThree();

            endpoint.Port++;
            listener.ChangeEndpoint(endpoint);
            AcceptThree();
        }

        public void Dispose()
        {
            listener.Stop();
        }
    }
}
