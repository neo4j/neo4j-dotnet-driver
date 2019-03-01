// Copyright (c) 2002-2019 "Neo4j,"
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
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Logging;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Metrics;
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver;

namespace Neo4j.Driver.Internal.Connector
{
    internal class SocketConnection : IConnection
    {
        private readonly ISocketClient _client;
        private IBoltProtocol _boltProtocol;
        private readonly IAuthToken _authToken;
        private readonly string _userAgent;
        private readonly IMessageResponseHandler _responseHandler;

        private readonly Queue<IRequestMessage> _messages = new Queue<IRequestMessage>();
        internal IReadOnlyList<IRequestMessage> Messages => _messages.ToList();

        private readonly PrefixLogger _logger;

        private string _id;
        private readonly string _idPrefix;

        public SocketConnection(Uri uri, ConnectionSettings connectionSettings, BufferSettings bufferSettings,
            IConnectionListener metricsListener = null, IDriverLogger logger = null)
        {
            _idPrefix = $"conn-{uri.Host}:{uri.Port}-";
            _id = $"{_idPrefix}{UniqueIdGenerator.GetId()}";
            _logger = new PrefixLogger(logger, FormatPrefix(_id));

            _client = new SocketClient(uri, connectionSettings.SocketSettings, bufferSettings, metricsListener, _logger);
            _authToken = connectionSettings.AuthToken;
            _userAgent = connectionSettings.UserAgent;
            Server = new ServerInfo(uri);

            _responseHandler = new MessageResponseHandler(_logger);
        }

        // for test only
        internal SocketConnection(ISocketClient socketClient, IAuthToken authToken,
            string userAgent, IDriverLogger logger, IServerInfo server,
            IMessageResponseHandler messageResponseHandler = null)
        {
            Throw.ArgumentNullException.IfNull(socketClient, nameof(socketClient));
            Throw.ArgumentNullException.IfNull(authToken, nameof(authToken));
            Throw.ArgumentNullException.IfNull(userAgent, nameof(userAgent));
            Throw.ArgumentNullException.IfNull(server, nameof(server));

            _client = socketClient;
            _authToken = authToken;
            _userAgent = userAgent;
            Server = server;

            _id = $"{_idPrefix}{UniqueIdGenerator.GetId()}";
            _logger = new PrefixLogger(logger, FormatPrefix(_id));
            _responseHandler = messageResponseHandler ?? new MessageResponseHandler(logger);
        }

        public AccessMode? Mode { get; set; }

        public async Task InitAsync()
        {
            _boltProtocol = await _client.ConnectAsync().ConfigureAwait(false);
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

            // send
            await _client.SendAsync(_messages).ConfigureAwait(false);
            
            _messages.Clear();
        }

        private async Task ReceiveAsync()
        {
            if (_responseHandler.UnhandledMessageSize == 0)
            {
                // nothing to receive
                return;
            }

            // receive
            await _client.ReceiveAsync(_responseHandler).ConfigureAwait(false);
            
            AssertNoServerFailure();
        }

        public async Task ReceiveOneAsync()
        {
            await _client.ReceiveOneAsync(_responseHandler).ConfigureAwait(false);

            AssertNoServerFailure();
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
            _logger.Debug("Connection '{0}' renamed to '{1}'. The new name identifies the connection uniquely both on the client side and the server side.", _id, newConnId);
            _id = newConnId;
            _logger.Prefix = FormatPrefix(_id);
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
                    await _boltProtocol.LogoutAsync(this);
                }
                catch (Exception e)
                {
                    _logger.Debug($"Failed to logout user before closing connection due to error: {e.Message}");
                }
                await _client.StopAsync();
            }
            catch (Exception e)
            {
                // only log the exception if failed to close connection
                _logger.Error(e, $"Failed to close connection properly.");
            }
        }

        private void AssertNoServerFailure()
        {
            if (_responseHandler.HasError)
            {
                var error = _responseHandler.Error;
                _responseHandler.Error = null;
                throw error;
            }
        }

        public Task EnqueueAsync(IRequestMessage requestMessage, IMessageResponseCollector resultBuilder = null,
            IRequestMessage requestStreamingMessage = null)
        {

            _messages.Enqueue(requestMessage);
            _responseHandler.EnqueueMessage(requestMessage, resultBuilder);

            if (requestStreamingMessage != null)
            {
                _messages.Enqueue(requestStreamingMessage);
                _responseHandler.EnqueueMessage(requestStreamingMessage, resultBuilder);
            }

            return TaskHelper.GetCompletedTask();
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
}
