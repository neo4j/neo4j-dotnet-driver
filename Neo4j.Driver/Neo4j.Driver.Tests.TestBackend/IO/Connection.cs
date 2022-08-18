// Copyright (c) 2002-2022 "Neo4j,"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
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

internal class Connection : IConnection, IDisposable
{
    private readonly TcpListener _server;
    private TcpClient _clientConnection;
    private bool _disposed;

    public Connection(string address, uint port)
    {
        Trace.WriteLine("Creating Server");
        var localAddr = IPAddress.Parse(address);
        _server = new TcpListener(localAddr, (int) port);
        StartServer();
    }

    public NetworkStream ConnectionStream { get; set; }

    public bool Connected => _clientConnection.Connected;
    public int TimeOut => 1000;

    public async Task Open()
    {
        try
        {
            Trace.WriteLine("Starting to listen for Connections");

            _clientConnection = await _server.AcceptTcpClientAsync();
            _clientConnection.LingerState.Enabled = false;
            _clientConnection.LingerState.LingerTime = 0;
            _clientConnection.ReceiveTimeout = TimeOut;

            ConnectionStream = _clientConnection.GetStream();
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
        _clientConnection.Close();
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

    public void StartServer()
    {
        Trace.WriteLine("Starting TCP server");
        //Start listening for connection requests
        _server.Start();
    }

    public void StopServer()
    {
        Trace.WriteLine("Stopping TCP server");
        _server.Stop();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            Close();
            StopServer();
        }

        _disposed = true;
    }
}