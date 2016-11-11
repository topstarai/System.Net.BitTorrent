using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace System.Net.Http.Abstractions
{
    class TcpListenerAdapter
    {
        private TcpListener _tcpListener;

        public TcpListenerAdapter(IPEndPoint localEndpoint)
        {
            LocalEndpoint = localEndpoint;

            Initialize();
        }

        public IPEndPoint LocalEndpoint { get; private set; }

        public Task<TcpClientAdapter> AcceptTcpClientAsync()
        {
            return acceptTcpClientAsyncInternal();
        }
        private void Initialize()
        {
            _tcpListener = new TcpListener(LocalEndpoint);
        }

        private async Task<TcpClientAdapter> acceptTcpClientAsyncInternal()
        {
            var tcpClient = await _tcpListener.AcceptTcpClientAsync();
            return new TcpClientAdapter(tcpClient);
        }

        public void Start()
        {
            _tcpListener.Start();
        }

        public void Stop()
        {
            _tcpListener.Stop();
        }

        public Socket Socket
        {
            get
            {
                return _tcpListener.Server;
            }
        }

    }

    class TcpClientAdapter
    {
        private TcpClient tcpClient;

        public TcpClientAdapter(TcpClient tcpClient)
        {
            this.tcpClient = tcpClient;

            LocalEndPoint = (IPEndPoint)tcpClient.Client.LocalEndPoint;
            RemoteEndPoint = (IPEndPoint)tcpClient.Client.RemoteEndPoint;
        }

        public Stream GetInputStream()
        {
            return this.tcpClient.GetStream();
        }

        public Stream GetOutputStream()
        {
            return this.tcpClient.GetStream();
        }

        public void Dispose()
        {
#if IS_CORECLR
            this.tcpClient.Dispose();
#else
            this.tcpClient.Close();
#endif
        }
        public IPEndPoint LocalEndPoint
        {
            get;
            private set;
        }

        public IPEndPoint RemoteEndPoint
        {
            get;
            private set;
        }
    }
}