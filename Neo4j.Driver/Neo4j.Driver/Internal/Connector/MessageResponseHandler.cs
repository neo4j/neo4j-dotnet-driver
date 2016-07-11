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
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Connector
{
    internal class MessageResponseHandler : IMessageResponseHandler
    {
        private readonly ILogger _logger;
        private readonly Queue<IResultBuilder> _resultBuilders = new Queue<IResultBuilder>();
        private readonly Queue<IRequestMessage> _unhandledMessages = new Queue<IRequestMessage>();
        internal IResultBuilder CurrentResultBuilder { get; private set; }

        public int UnhandledMessageSize => _unhandledMessages.Count;

        public Neo4jException Error { get; internal set; }
        public bool HasError => Error != null;

        internal Queue<IResultBuilder> ResultBuilders => new Queue<IResultBuilder>(_resultBuilders);
        internal Queue<IRequestMessage> SentMessages => new Queue<IRequestMessage>(_unhandledMessages);

        public MessageResponseHandler()
        {
        }

        public MessageResponseHandler(ILogger logger)
        {
            _logger = logger;
        }

        public void HandleSuccessMessage(IDictionary<string, object> meta)
        {
            DequeueMessage();
            if (meta.ContainsKey("fields"))
            {
                // first success
                CurrentResultBuilder?.CollectFields(meta);
            }
            else
            {
                // second success
                // before summary method is called
                CurrentResultBuilder?.CollectSummaryMeta(meta);
            }
            Error = null;
            _logger?.Debug("S: ", new SuccessMessage(meta));
        }

        public void HandleRecordMessage(object[] fields)
        {
            CurrentResultBuilder?.CollectRecord(fields);
            _logger?.Debug("S: ", new RecordMessage(fields));
        }

        public void HandleFailureMessage(string code, string message)
        {
            DequeueMessage();
            var parts = code.Split('.');
            var classification = parts[1].ToLowerInvariant();
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
            _logger?.Debug("S: ", new FailureMessage(code, message));
        }

        public void HandleIgnoredMessage()
        {
            DequeueMessage();
            _logger?.Debug("S: ", new IgnoredMessage());
        }

        public void EnqueueMessage(IRequestMessage requestMessage, IResultBuilder resultBuilder = null)
        {
            _unhandledMessages.Enqueue(requestMessage);
            _resultBuilders.Enqueue(resultBuilder);
            if (requestMessage is ResetMessage)
            {
                Interrupt();
            }
        }

        private void DequeueMessage()
        {
            _unhandledMessages.Dequeue();
            CurrentResultBuilder = _resultBuilders.Dequeue();
            CurrentResultBuilder?.IsStreamingRecords(false);
        }

        private void Interrupt()
        {
            // when receiving a reset, we will interrupt to not saving any incoming records by clean the result builder.
            CurrentResultBuilder?.IsStreamingRecords(false);
            CurrentResultBuilder = null;
        }

        public void Clear()
        {
            _resultBuilders.Clear();
            _unhandledMessages.Clear();

            CurrentResultBuilder = null;
            Error = null;
        }
    }
}