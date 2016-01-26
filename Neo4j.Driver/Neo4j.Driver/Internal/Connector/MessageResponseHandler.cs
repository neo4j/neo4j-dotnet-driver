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
using System.Collections.Generic;
using Neo4j.Driver.Exceptions;
using Neo4j.Driver.Internal.messaging;
using Neo4j.Driver.Internal.result;

namespace Neo4j.Driver
{
    internal class MessageResponseHandler : IMessageResponseHandler
    {
        private readonly Queue<ResultBuilder> _resultBuilders = new Queue<ResultBuilder>();
        protected readonly Queue<IMessage> _sentMessages = new Queue<IMessage>() ;
        private ResultBuilder _currentResultBuilder;
        public Neo4jException Error { get; internal set; }
        public bool HasError => Error != null;

        public void HandleSuccessMessage(IDictionary<string, object> meta)
        {
            _sentMessages.Dequeue();
            _currentResultBuilder = _resultBuilders.Dequeue();
            _currentResultBuilder?.CollectMeta(meta);
        }

        public void HandleRecordMessage(dynamic[] fields)
        {
            //TODO: Should error if no keys??
            _currentResultBuilder.Record( fields);
        }

        public void HandleFailureMessage(string code, string message)
        {
            string[] parts = code.Split('.');
            string classification = parts[1].ToLowerInvariant();
            switch (classification)
            {
                case "clienterror":
                    Error = new ClientException(code, message);
                    break;
                case "transienterror":
                    Error = new TransientException(code, message);
                    break;
                default:
                    Error = new DatabaseException(code, message);
                    break;
            }
            _sentMessages.Dequeue();
            _resultBuilders.Dequeue();
        }

        public void HandleIgnoredMessage()
        {
            _sentMessages.Dequeue();
            _resultBuilders.Dequeue();
        }

        public void Register(IMessage message, ResultBuilder resultBuilder = null)
        {
            _sentMessages.Enqueue(message);
            _resultBuilders.Enqueue(resultBuilder);
            
        }

        public bool QueueIsEmpty()
        {
            return _sentMessages.Count == 0;
        }
    }
}