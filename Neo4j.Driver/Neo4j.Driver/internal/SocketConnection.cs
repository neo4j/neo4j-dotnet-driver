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
using Neo4j.Driver.Internal.messaging;
using Neo4j.Driver.Internal.result;

namespace Neo4j.Driver
{
    internal class SocketConnection : IConnection
    {
        private readonly ISocketClient _client;

        private readonly Queue<IMessage> _messages = new Queue<IMessage>();
        private readonly IMessageResponseHandler _messageHandler = new MessageResponseHandler();

        private int _requestCounter;

        public SocketConnection(ISocketClient socketClient)
        {
            _client = socketClient;
            var t = _client.Start();
            t.Wait();

            // add init message by default
            Init("dotNet-driver/1.0.0");
        }

        public SocketConnection(Uri url, Config config)
            : this(new SocketClient(url, config))
        {}

        internal IReadOnlyList<IMessage> Messages => _messages.ToList();

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

        public void Run(ResultBuilder resultBuilder, string statement, IDictionary<string, object> statementParameters = null)
        {
            var runMessage = new RunMessage(statement, statementParameters);
            Enqueue(runMessage, resultBuilder);
//            _messageHandler.RegisterResultBuilder(resultBuilder);
            
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
            _requestCounter = 0;
            _messages.Clear();
        }

        private void Init(string clientName)
        {
            Enqueue(new InitMessage(clientName));
            //_client.Send(initMessage);
        }

        public void PullAll(ResultBuilder resultBuilder)
        {
            Enqueue(new PullAllMessage(), resultBuilder);
//            _messageHandler.RegisterResultBuilder(resultBuilder);
            //_resultCollector.AddMessage(PullAllMessage)
        }

        private int Enqueue(IMessage message, ResultBuilder resultBuilder = null)
        {
            _messages.Enqueue(message);
            _requestCounter ++;
            _messageHandler.Register(message, resultBuilder);
            return _requestCounter;
        }
    }
}