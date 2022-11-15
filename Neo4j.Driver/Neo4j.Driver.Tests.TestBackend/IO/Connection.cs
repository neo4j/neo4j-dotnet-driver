// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License"):
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Neo4j.Driver.Tests.TestBackend;

internal interface IConnection
{
    bool Connected { get; }
    NetworkStream ConnectionStream { get; }
    int TimeOut { get; }
    Task Open();
    void Close();
    void StartServer();
    void StopServer();
    bool DataAvailable();
}

internal class Connection : IConnection, IDisposable
{
    public Connection(string address, uint port)
    {
        Trace.WriteLine("Creating Server");
        var localAddr = IPAddress.Parse(address);
        Server = new TcpListener(localAddr, (int)port);
        StartServer();
    }

    private TcpListener Server { get; }
    private TcpClient ClientConnection { get; set; }
    private bool Disposed { get; set; }
    private string Uri { get; set; }
    private int port { get; set; }
    public bool Connected => ClientConnection.Connected;
    public int TimeOut => 1000;

    public NetworkStream ConnectionStream { get; set; }

    public void StartServer()
    {
        Trace.WriteLine("Starting TCP server");
        Server.Start(); //Start listening for connection requests
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

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~Connection()
    {
        Dispose(false);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (Disposed)
        {
            return;
        }

        if (disposing)
        {
            Close();
            StopServer();
        }

        Disposed = true;
    }
}
