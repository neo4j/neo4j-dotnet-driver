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
using Neo4j.Driver.Exceptions;
using Neo4j.Driver.Internal.messaging;
using Neo4j.Driver.Internal.result;

namespace Neo4j.Driver
{
    internal class SocketConnection : IConnection
    {
        private readonly ISocketClient _client;
        private readonly IMessageResponseHandler _messageHandler;

        private readonly Queue<IMessage> _messages = new Queue<IMessage>();
        internal IReadOnlyList<IMessage> Messages => _messages.ToList();

        public SocketConnection(ISocketClient socketClient, IMessageResponseHandler messageResponseHandler = null)
        {
            Throw.ArgumentNullException.IfNull(socketClient, nameof(socketClient));
            _messageHandler = messageResponseHandler ?? new MessageResponseHandler();

            _client = socketClient;
            var t = _client.Start();
            t.Wait();

            // add init message by default
            Init("dotNet-driver/1.0.0");
        }

        public SocketConnection(Uri url, Config config)
            : this(new SocketClient(url, config))
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
        }

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

        protected virtual void Dispose(bool isDisposing)
        {
            if (!isDisposing)
                return;

            var t = _client.Stop();
            t.Wait();
        }

        private void ClearQueue()
        {
            _messages.Clear();
        }

        private void Init(string clientName)
        {
            Enqueue(new InitMessage(clientName));
        }

        private void Enqueue(IMessage message, ResultBuilder resultBuilder = null)
        {
            _messages.Enqueue(message);
            _messageHandler.Register(message, resultBuilder);
        }
    }
}