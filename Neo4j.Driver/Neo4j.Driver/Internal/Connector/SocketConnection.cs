// Copyright (c) 2002-2016 "Neo Technology,"
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
        private readonly IMessageResponseHandler _responseHandler;

        private readonly Queue<IRequestMessage> _messages = new Queue<IRequestMessage>();
        internal IReadOnlyList<IRequestMessage> Messages => _messages.ToList();

        private volatile bool _interrupted;
        private readonly object _syncLock = new object();

        public SocketConnection(ISocketClient socketClient, IAuthToken authToken, ILogger logger,
            IMessageResponseHandler messageResponseHandler = null)
        {
            Throw.ArgumentNullException.IfNull(socketClient, nameof(socketClient));
            _responseHandler = messageResponseHandler ?? new MessageResponseHandler(logger);

            _client = socketClient;
            Task.Run(() => _client.Start()).Wait();

            // add init requestMessage by default
            Enqueue(new InitMessage("neo4j-dotnet/1.1", authToken.AsDictionary()));
            Sync();
        }

        public SocketConnection(Uri uri, IAuthToken authToken, Config config)
            : this(new SocketClient(uri, config), authToken, config?.Logger)
        {
        }

        public void Sync()
        {
            Send();
            Receive();
        }

        public void Send()
        {
            lock (_syncLock)
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

        public void Run(IMessageResponseCollector resultBuilder, string statement, IDictionary<string, object> paramters=null)
        {
            var runMessage = new RunMessage(statement, paramters);
            Enqueue(runMessage, resultBuilder);
        }

        public void PullAll(IMessageResponseCollector resultBuilder)
        {
            Enqueue(new PullAllMessage(), resultBuilder);
        }

        public void DiscardAll()
        {
            Enqueue(new DiscardAllMessage());
        }

        public void Reset()
        {
            Enqueue(new ResetMessage());
        }

        public void ResetAsync()
        {
            if (!_interrupted)
            {
                _interrupted = true;
                Enqueue(new ResetMessage(), new ResetCollector(() => { _interrupted = false; }));
                Send();
            }
        }

        public bool IsOpen => _client.IsOpen;
        public bool HasUnrecoverableError { get; private set; }
        public bool IsHealthy => IsOpen && !HasUnrecoverableError;

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

            Task.Run(() => _client.Stop()).Wait();
        }

        private void AssertNoServerFailure()
        {
            if (_responseHandler.HasError)
            {
                if (IsRecoverableError(_responseHandler.Error))
                {
                    if (!_interrupted)
                    {
                        Enqueue(new AckFailureMessage());
                    }
                }
                else
                {
                    HasUnrecoverableError = true;
                }
                var error = _responseHandler.Error;
                _responseHandler.Error = null;
                throw error;
            }
        }

        private bool IsRecoverableError(Neo4jException error)
        {
            return error is ClientException || error is TransientException;
        }

        private void Enqueue(IRequestMessage requestMessage, IMessageResponseCollector resultBuilder = null)
        {
            lock (_syncLock)
            {
                _messages.Enqueue(requestMessage);
                _responseHandler.EnqueueMessage(requestMessage, resultBuilder);
            }
        }
    }
}