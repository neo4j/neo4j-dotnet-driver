// Copyright (c) "Neo4j"
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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Logging;
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.Internal.Util;

namespace Neo4j.Driver.Internal.Connector;

internal sealed class SocketConnection : IConnection
{
    private readonly IAuthToken _authToken;

    private readonly ISocketClient _client;
    private readonly string _idPrefix;

    private readonly PrefixLogger _logger;

    private readonly Queue<IRequestMessage> _messages = new();
    private readonly SemaphoreSlim _recvLock = new(1, 1);
    private readonly IResponsePipeline _responsePipeline;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly string _userAgent;

    private string _id;

    public SocketConnection(Uri uri, ConnectionSettings connectionSettings,
        BufferSettings bufferSettings, IDictionary<string, string> routingContext, ILogger logger = null)
    {
        _idPrefix = $"conn-{uri.Host}:{uri.Port}-";
        _id = $"{_idPrefix}{UniqueIdGenerator.GetId()}";
        _logger = new PrefixLogger(logger, FormatPrefix(_id));

        _client = new SocketClient(this, uri, connectionSettings.SocketSettings, bufferSettings, _logger);
        _authToken = connectionSettings.AuthToken;
        _userAgent = connectionSettings.UserAgent;
        _serverInfo = new ServerInfo(uri);

        _responsePipeline = new ResponsePipeline(_logger);
        RoutingContext = routingContext;
    }

    // for test only
    internal SocketConnection(ISocketClient socketClient, IAuthToken authToken,
        string userAgent, ILogger logger, ServerInfo server,
        IResponsePipeline responsePipeline = null)
    {
        Throw.ArgumentNullException.IfNull(socketClient, nameof(socketClient));
        Throw.ArgumentNullException.IfNull(authToken, nameof(authToken));
        Throw.ArgumentNullException.IfNull(userAgent, nameof(userAgent));
        Throw.ArgumentNullException.IfNull(server, nameof(server));

        _client = socketClient;
        _authToken = authToken;
        _userAgent = userAgent;
        _serverInfo = server;
        RoutingContext = null;

        _id = $"{_idPrefix}{UniqueIdGenerator.GetId()}";
        _logger = new PrefixLogger(logger, FormatPrefix(_id));
        _responsePipeline = responsePipeline ?? new ResponsePipeline(logger);
    }

    internal IReadOnlyList<IRequestMessage> Messages => _messages.ToList();

    public IDictionary<string, string> RoutingContext { get; set; }

    public AccessMode? Mode { get; private set; }

    public string Database { get; private set; }

    public BoltProtocolVersion Version => _client.Version;

    public IBoltProtocol BoltProtocol => _boltProtocol;

    public void Configure(string database, AccessMode? mode)
    {
        Mode = mode;
        Database = database;
    }

    public async Task InitAsync(CancellationToken cancellationToken = default)
    {
        await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await _client.ConnectAsync(RoutingContext, cancellationToken).ConfigureAwait(false);
            _boltProtocol = BoltProtocolFactory.ForVersion(_client.Version);
        }
        finally
        {
            _sendLock.Release();
        }

        await _boltProtocol.LoginAsync(this, _userAgent, _authToken).ConfigureAwait(false);
    }

    public async Task SyncAsync()
    {
        await SendAsync().ConfigureAwait(false);
        await ReceiveAsync().ConfigureAwait(false);
    }

    public async Task SendAsync()
    {
        if (_messages.Count == 0)
            // nothing to send
            return;

        await _sendLock.WaitAsync().ConfigureAwait(false);
        try
        {
            // send
            await _client.SendAsync(_messages).ConfigureAwait(false);

            _messages.Clear();
        }
        finally
        {
            _sendLock.Release();
        }
    }

    public async Task ReceiveOneAsync()
    {
        await _recvLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_responsePipeline.HasNoPendingMessages)
                return;

            await _client.ReceiveOneAsync(_responsePipeline).ConfigureAwait(false);

            _responsePipeline.AssertNoFailure();
        }
        finally
        {
            _recvLock.Release();
        }
    }

    public Task ResetAsync()
    {
        return _boltProtocol.ResetAsync(this);
    }

    public bool IsOpen => _client.IsOpen;
    private ServerInfo _serverInfo;
    public IServerInfo Server => _serverInfo;

    private IBoltProtocol _boltProtocol;

    public bool UtcEncodedDateTime { get; private set; }

    public void UpdateId(string newConnId)
    {
        _logger.Debug(
            "Connection '{0}' renamed to '{1}'. The new name identifies the connection uniquely both on the client side and the server side.",
            _id, newConnId);
        _id = newConnId;
        _logger.Prefix = FormatPrefix(_id);
    }

    public void UpdateVersion(ServerVersion newVersion)
    {
        _serverInfo.Update(_client.Version, newVersion.Agent);
    }

    public Task DestroyAsync()
    {
        return CloseAsync();
    }

    public async Task CloseAsync()
    {
        try
        {
            try
            {
                if (_boltProtocol != null)
                    await _boltProtocol.LogoutAsync(this).ConfigureAwait(false);
            }
            catch (Exception e) when (e.HasCause<ObjectDisposedException>())
            {
                // we'll ignore this error since the underlying socket is disposed earlier,
                // mostly because of an error.
            }
            catch (Exception e)
            {
                _logger.Debug($"Failed to logout user before closing connection due to error: {e.Message}");
            }

            await _client.StopAsync().ConfigureAwait(false);
        }
        catch (Exception e)
        {
            // only log the exception if failed to close connection
            _logger.Warn(e, "Failed to close connection properly.");
        }
    }

    public Task EnqueueAsync(IRequestMessage message1, IResponseHandler handler1,
        IRequestMessage message2 = null,
        IResponseHandler handler2 = null)
    {
        _sendLock.Wait();

        try
        {
            _messages.Enqueue(message1);
            _responsePipeline.Enqueue(handler1);

            if (message2 != null)
            {
                _messages.Enqueue(message2);
                _responsePipeline.Enqueue(handler2);
            }
        }
        finally
        {
            _sendLock.Release();
        }

        return Task.CompletedTask;
    }

    public void SetReadTimeoutInSeconds(int seconds)
    {
        _client.SetReadTimeoutInSeconds(seconds);
    }

    public void SetUseUtcEncodedDateTime()
    {
        UtcEncodedDateTime = true;
        _client.UseUtcEncoded();
    }

    private async Task ReceiveAsync()
    {
        await _recvLock.WaitAsync().ConfigureAwait(false);

        try
        {
            if (_responsePipeline.HasNoPendingMessages)
                return;

            await _client.ReceiveAsync(_responsePipeline).ConfigureAwait(false);

            _responsePipeline.AssertNoFailure();
        }
        finally
        {
            _recvLock.Release();
        }
    }

    public override string ToString()
    {
        return _id;
    }

    private static string FormatPrefix(string id)
    {
        return $"[{id}]";
    }
}