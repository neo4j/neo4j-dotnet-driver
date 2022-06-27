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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Logging;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Metrics;
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver;
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.Messaging.V4;
using Neo4j.Driver.Internal.Util;

namespace Neo4j.Driver.Internal.Connector
{
    internal class SocketConnection : IConnection
    {
        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _recvLock = new SemaphoreSlim(1, 1);

        private readonly ISocketClient _client;
        private IBoltProtocol _boltProtocol;
        private readonly IAuthToken _authToken;
        private readonly string _userAgent;
        private readonly IResponsePipeline _responsePipeline;

        private readonly Queue<IRequestMessage> _messages = new Queue<IRequestMessage>();
        internal IReadOnlyList<IRequestMessage> Messages => _messages.ToList();

        private readonly PrefixLogger _logger;

        private string _id;
        private readonly string _idPrefix;

        public IDictionary<string, string> RoutingContext { get; set; }

        public SocketConnection(Uri uri, ConnectionSettings connectionSettings, BufferSettings bufferSettings, IDictionary<string, string> routingContext, ILogger logger = null)
        {
            _idPrefix = $"conn-{uri.Host}:{uri.Port}-";
            _id = $"{_idPrefix}{UniqueIdGenerator.GetId()}";
            _logger = new PrefixLogger(logger, FormatPrefix(_id));

            _client = new SocketClient(uri, connectionSettings.SocketSettings, bufferSettings, _logger);
            _authToken = connectionSettings.AuthToken;
            _userAgent = connectionSettings.UserAgent;
            Server = new ServerInfo(uri);

            _responsePipeline = new ResponsePipeline(_logger);
            RoutingContext = routingContext;
        }

        // for test only
        internal SocketConnection(ISocketClient socketClient, IAuthToken authToken,
            string userAgent, ILogger logger, IServerInfo server,
            IResponsePipeline responsePipeline = null)
        {
            Throw.ArgumentNullException.IfNull(socketClient, nameof(socketClient));
            Throw.ArgumentNullException.IfNull(authToken, nameof(authToken));
            Throw.ArgumentNullException.IfNull(userAgent, nameof(userAgent));
            Throw.ArgumentNullException.IfNull(server, nameof(server));

            _client = socketClient;
            _authToken = authToken;
            _userAgent = userAgent;
            Server = server;
            RoutingContext = null;

            _id = $"{_idPrefix}{UniqueIdGenerator.GetId()}";
            _logger = new PrefixLogger(logger, FormatPrefix(_id));
            _responsePipeline = responsePipeline ?? new ResponsePipeline(logger);            
        }

        public AccessMode? Mode { get; set; }

        public string Database { get; set; }

        public async Task InitAsync(CancellationToken cancellationToken = default)
        {
            await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                _boltProtocol = await _client.ConnectAsync(RoutingContext, cancellationToken).ConfigureAwait(false);
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
            {
                // nothing to send
                return;
            }

            _sendLock.Wait();
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

        private async Task ReceiveAsync()
        {
            _recvLock.Wait();

            try
            {
                if (_responsePipeline.HasNoPendingMessages)
                {
                    return;
                }

                await _client.ReceiveAsync(_responsePipeline).ConfigureAwait(false);

                _responsePipeline.AssertNoFailure();
            }
            finally
            {
                _recvLock.Release();
            }
        }

        public async Task ReceiveOneAsync()
        {
            _recvLock.Wait();
            try
            {
                if (_responsePipeline.HasNoPendingMessages)
                {
                    return;
                }

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
        public IServerInfo Server { get; set; }

        public IBoltProtocol BoltProtocol
        {
            get => _boltProtocol;
            set => _boltProtocol = value;
        }

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
            if (Server is ServerInfo info)
            {
				info.Update(_boltProtocol.Version, newVersion.Agent);				
			}
            else
            {
                throw new InvalidOperationException(
                    $"Current Server instance of type {Server.GetType().Name} does not allow version updating.");
            }
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
                    {
                        await _boltProtocol.LogoutAsync(this).ConfigureAwait(false);
                    }
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
                _logger.Warn(e, $"Failed to close connection properly.");
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
                _responsePipeline.Enqueue(message1, handler1);

                if (message2 != null)
                {
                    _messages.Enqueue(message2);
                    _responsePipeline.Enqueue(message2, handler2);
                }
            }
            finally
            {
                _sendLock.Release();
            }

            return Task.CompletedTask;
        }

        public override string ToString()
        {
            return _id;
        }

        private static string FormatPrefix(string id)
        {
            return $"[{id}]";
        }

		public void SetRecvTimeOut(int seconds)
		{
			_client.SetRecvTimeOut(seconds);			
		}

        public void SetUseUtcEncodedDateTime()
        {
            _client.SetUseUtcEncodedDateTime(_boltProtocol);
        }
	}
}