using System;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal interface IConnection
    {
        bool Connected { get; }
        NetworkStream ConnectionStream { get; }
        Task Open();
        void Close();
        void StartServer();
        void StopServer();
        bool DataAvailable();
        int TimeOut { get; }
    }

    internal class Connection : IConnection, IDisposable
    {
        private TcpListener Server { get; }
        private TcpClient ClientConnection { get; set; }
        private bool Disposed { get; set; } = false;
        private string Uri { get; set; }
        private int port { get; set; }
        public void Dispose() => Dispose(true);
        public bool Connected { get { return ClientConnection.Connected; } }
        public int TimeOut { get { return 1000; } }

        public NetworkStream ConnectionStream { get; set; }

        public Connection(string address, uint port)
        {
            Trace.WriteLine("Creating Server");
            IPAddress localAddr = IPAddress.Parse(address);
            Server = new TcpListener(localAddr, (int)port);   
            StartServer();
        }

        public void StartServer()
        {
            Trace.WriteLine("Starting TCP server");
            Server.Start();     //Start listening for connection requests
        }

        public void StopServer()
        {
            Trace.WriteLine("Stopping TCP server");
            Server.Stop();
        }

        public bool DataAvailable()
        {
            return ClientConnection.Available > 0;
        }

        public async Task Open()
        {
            try
            {   
                Trace.WriteLine("Starting to listen for Connections");
                
                ClientConnection = await Server.AcceptTcpClientAsync().ConfigureAwait(false);
                ClientConnection.LingerState.Enabled = false;
                ClientConnection.LingerState.LingerTime = 0;
                ClientConnection.ReceiveTimeout = TimeOut;

                ConnectionStream = ClientConnection.GetStream();
                ConnectionStream.ReadTimeout = TimeOut;
                ConnectionStream.WriteTimeout = TimeOut;

                Trace.WriteLine("Connected");
            }
            catch (SocketException e)
            {
                Trace.WriteLine($"SocketException: {e}");
                Close();
            }
        }

        public void Close()
        {
            ClientConnection.Close();
            ConnectionStream.Dispose();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Disposed)
                return;

            if (disposing)
            {
                Close();
                StopServer();
            }

            Disposed = true;
        }
    }
}

