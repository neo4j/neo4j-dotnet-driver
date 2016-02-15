//  Copyright (c) 2002-2016 "Neo Technology,"
//  Network Engine for Objects in Lund AB [http://neotechnology.com]
// 
//  This file is part of Neo4j.
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Neo4j.Driver.Exceptions;
using Neo4j.Driver.Extensions;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Result;

namespace Neo4j.Driver.Internal.Connector
{
    internal class SocketConnection : IConnection
    {
        private readonly ISocketClient _client;
        private readonly IMessageResponseHandler _messageHandler;

        private readonly Queue<IRequestMessage> _messages = new Queue<IRequestMessage>();
        internal IReadOnlyList<IRequestMessage> Messages => _messages.ToList();

        public SocketConnection(ISocketClient socketClient, ILogger logger, IMessageResponseHandler messageResponseHandler = null)
        {
            Throw.ArgumentNullException.IfNull(socketClient, nameof(socketClient));
            _messageHandler = messageResponseHandler ?? new MessageResponseHandler(logger);

            _client = socketClient;
            Task.Run(() => _client.Start()).Wait();

            // add init requestMessage by default
            Enqueue(new InitMessage("neo4j-dotnet/1.0.0"));
        }

        public SocketConnection(Uri url, Config config)
            : this(new SocketClient(url, config), config?.Logger)
        {
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Sync()
        {
            if (_messages.Count == 0)
            {
                return;
            }

            _client.Send(_messages, _messageHandler);
            ClearQueue(); // clear sending queue

            if (_messageHandler.HasError)
            {
                Enqueue(new ResetMessage());
                throw _messageHandler.Error;
            }

        }

        public bool HasUnrecoverableError
         => _messageHandler.Error is TransientException || _messageHandler.Error is DatabaseException;

        public void Run(ResultBuilder resultBuilder, string statement,
            IDictionary<string, object> statementParameters = null)
        {
            var runMessage = new RunMessage(statement, statementParameters);
            Enqueue(runMessage, resultBuilder);
        }

        public void PullAll(ResultBuilder resultBuilder)
        {
            Enqueue(new PullAllMessage(), resultBuilder);
        }

        public void DiscardAll()
        {
            Enqueue(new DiscardAllMessage());
        }

        public void Reset()
        {
            ClearQueue();
            _messageHandler.Clear();
            Enqueue(new ResetMessage());
        }

        public bool IsOpen => _client.IsOpen;
        

        protected virtual void Dispose(bool isDisposing)
        {
            if (!isDisposing)
                return;

            Task.Run(() => _client.Stop()).Wait();
        }

        private void ClearQueue()
        {
            _messages.Clear();
        }

        private void Enqueue(IRequestMessage requestMessage, ResultBuilder resultBuilder = null)
        {
            _messages.Enqueue(requestMessage);
            _messageHandler.Register(requestMessage, resultBuilder);
        }
    }
}