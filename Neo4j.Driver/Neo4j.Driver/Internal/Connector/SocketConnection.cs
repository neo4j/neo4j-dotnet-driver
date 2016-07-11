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

        public SocketConnection(Uri url, IAuthToken authToken, Config config)
            : this(new SocketClient(url, config), authToken, config?.Logger)
        {
        }

        internal IReadOnlyList<IRequestMessage> Messages => _messages.ToList();

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void SendAndReceive(int unhandledMessageCount = 0)
        {
            if (_messages.Count == 0)
            {
                return;
            }

            // blocking to send
            _client.Send(_messages);
            ClearQueue(); // clear sending queue
            // blocking to receive
            _client.Receive(_responseHandler, unhandledMessageCount);

            if (_responseHandler.HasError)
            {
                OnResponseHasError();
            }
        }

        private void OnResponseHasError()
        {
            Enqueue(new AckFailureMessage());
            throw _responseHandler.Error;
        }

        public void Sync()
        {
            SendAndReceive();
        }

        public void SyncRun()
        {
            SendAndReceive(1); // blocking to receive unitl 1 message unhandled left (PULL_ALL)
        }

        public bool HasUnrecoverableError
            => _responseHandler.Error is DatabaseException;

        public void Run(IResultBuilder resultBuilder, string statement, IDictionary<string, object> paramters=null)
        {
            var runMessage = new RunMessage(statement, paramters);
            Enqueue(runMessage, resultBuilder);
        }

        public void PullAll(IResultBuilder resultBuilder)
        {
            Enqueue(new PullAllMessage(), resultBuilder);
            resultBuilder.ReceiveOneRecordMessageFunc = () => _client.ReceiveOneRecordMessage(_responseHandler, OnResponseHasError);
        }

        public void DiscardAll()
        {
            Enqueue(new DiscardAllMessage());
        }

        public void Reset()
        {
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

        private void Enqueue(IRequestMessage requestMessage, IResultBuilder resultBuilder = null)
        {
            _messages.Enqueue(requestMessage);
            _responseHandler.EnqueueMessage(requestMessage, resultBuilder);
        }
    }
}