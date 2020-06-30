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

        public NetworkStream ConnectionStream { get; set; }

        public Connection(string address, int port)
        {
            Trace.WriteLine("Creating Server");
            IPAddress localAddr = IPAddress.Parse(address);
            Server = new TcpListener(localAddr, port);
        }

        public async Task Open()
        {
            try
            {
                Trace.WriteLine("Starting TCP server");
                Server.Start();     //Start listening for connection requests
                Trace.WriteLine("Starting to listen for Connections");
                ClientConnection = await Server.AcceptTcpClientAsync().ConfigureAwait(false);
                ConnectionStream = ClientConnection.GetStream();
                Trace.WriteLine("Connected");
            }
            catch (SocketException e)
            {
                Trace.WriteLine($"SocketException: {e}");
                Close();
            }
        }

        private void Close()
        {
            ConnectionStream.Dispose();
            ClientConnection.Close();
            Server.Stop();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Disposed)
                return;

            if (disposing)
            {
                Close();
            }

            Disposed = true;
        }
    }
}

