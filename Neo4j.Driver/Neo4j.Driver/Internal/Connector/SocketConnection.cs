// Copyright (c) 2002-2017 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
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
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Connector
{
    internal class SocketConnection : IConnection
    {
        private readonly ISocketClient _client;
        private readonly IAuthToken _authToken;
        private readonly TimeSpan _connectionTimeout;
        private readonly string _userAgent;
        private readonly IMessageResponseHandler _responseHandler;

        private readonly Queue<IRequestMessage> _messages = new Queue<IRequestMessage>();
        internal IReadOnlyList<IRequestMessage> Messages => _messages.ToList();

        private readonly ILogger _logger;

        public SocketConnection(Uri uri, ConnectionSettings connectionSettings, ILogger logger)
            : this(new SocketClient(uri, connectionSettings.EncryptionManager, connectionSettings.SocketKeepAliveEnabled, connectionSettings.Ipv6Enabled, logger),
                  connectionSettings.AuthToken, connectionSettings.ConnectionTimeout, connectionSettings.UserAgent,
                  logger, new ServerInfo(uri))
        {
        }

        internal SocketConnection(ISocketClient socketClient, IAuthToken authToken,
            TimeSpan connectionTimeout, string userAgent, ILogger logger, IServerInfo server,
            IMessageResponseHandler messageResponseHandler = null)
        {
            Throw.ArgumentNullException.IfNull(socketClient, nameof(socketClient));
            Throw.ArgumentNullException.IfNull(authToken, nameof(authToken));
            Throw.ArgumentNullException.IfNull(userAgent, nameof(userAgent));
            Throw.ArgumentNullException.IfNull(server, nameof(server));

            _client = socketClient;
            _authToken = authToken;
            _connectionTimeout = connectionTimeout;
            _userAgent = userAgent;
            Server = server;

            _logger = logger;
            _responseHandler = messageResponseHandler ?? new MessageResponseHandler(logger);
        }

        public void Init()
        {

            var connected = Task.Run(() => _client.Start()).Wait(_connectionTimeout);
            if (!connected)
            {
                throw new IOException(
                    $"Failed to connect to the server {Server.Address} within connection timeout {_connectionTimeout.TotalMilliseconds}ms");
            }

            Init(_authToken);
        }

        private void Init(IAuthToken authToken)
        {
            var initCollector = new InitCollector();
            Enqueue(new InitMessage(_userAgent, authToken.AsDictionary()), initCollector);
            Sync();
            ((ServerInfo)Server).Version = initCollector.Server;
            _client.UpdatePackStream(initCollector.Server);
        }

        public void Sync()
        {
            Send();
            Receive();
        }

        public void Send()
        {
            if (_messages.Count == 0)
            {
                // nothing to send
                return;
            }
            // blocking to send
            _client.Send(_messages);

            _messages.Clear();
        }

        private void Receive()
        {
            if (_responseHandler.UnhandledMessageSize == 0)
            {
                // nothing to receive
                return;
            }

            // blocking to receive
            _client.Receive(_responseHandler);
            AssertNoServerFailure();
        }

        public void ReceiveOne()
        {
            _client.ReceiveOne(_responseHandler);
            AssertNoServerFailure();
        }

        public void Run(string statement, IDictionary<string, object> paramters = null, IMessageResponseCollector resultBuilder = null, bool pullAll = true)
        {
            if (pullAll)
            {
                Enqueue(new RunMessage(statement, paramters), resultBuilder, new PullAllMessage());
            }
            else
            {
                Enqueue(new RunMessage(statement, paramters), resultBuilder, new DiscardAllMessage());
            }
        }

        public void Reset()
        {
            Enqueue(new ResetMessage());
        }

        public void AckFailure()
        {

            Enqueue(new AckFailureMessage());
        }

        public bool IsOpen => _client.IsOpen;
        public IServerInfo Server { get; }

        public void Close()
        {
            Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (!isDisposing)
                return;

            try
            {
                _client.Dispose();
            }
            catch (Exception e)
            {
                // only log the exception if failed to close connection
                _logger.Error($"Failed to close connection properly due to error: {e.Message}", e);
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

        private void Enqueue(IRequestMessage requestMessage, IMessageResponseCollector resultBuilder = null,
            IRequestMessage requestStreamingMessage = null)
        {

            _messages.Enqueue(requestMessage);
            _responseHandler.EnqueueMessage(requestMessage, resultBuilder);

            if (requestStreamingMessage != null)
            {
                _messages.Enqueue(requestStreamingMessage);
                _responseHandler.EnqueueMessage(requestStreamingMessage, resultBuilder);
            }
        }
    }
}