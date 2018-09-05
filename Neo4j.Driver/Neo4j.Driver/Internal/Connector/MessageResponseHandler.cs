﻿// Copyright (c) 2002-2018 "Neo4j,"
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
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.V1;
using static Neo4j.Driver.Internal.ErrorExtensions;
using static Neo4j.Driver.Internal.Messaging.IgnoredMessage;

namespace Neo4j.Driver.Internal.Connector
{
    internal class MessageResponseHandler : IMessageResponseHandler
    {
        private readonly ILogger _logger;
        private readonly Queue<IMessageResponseCollector> _resultBuilders = new Queue<IMessageResponseCollector>();
        private readonly Queue<IRequestMessage> _unhandledMessages = new Queue<IRequestMessage>();

        public IMessageResponseCollector CurrentResponseCollector { get; private set; }
        public int UnhandledMessageSize => _unhandledMessages.Count;

        private readonly object _syncLock = new object();

        public Neo4jException Error { get; set; }
        public bool HasError => Error != null;
        public bool HasProtocolViolationError => HasError && Error is ProtocolException;

        internal Queue<IMessageResponseCollector> ResultBuilders => new Queue<IMessageResponseCollector>(_resultBuilders);
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
            if (meta.ContainsKey(Bookmark.BookmarkKey))
            {
                CurrentResponseCollector?.CollectBookmark(meta);
            }

            if (meta.ContainsKey("fields"))
            {
                // first success
                CurrentResponseCollector?.CollectFields(meta);
            }
            else
            {
                // second success
                // before summary method is called
                CurrentResponseCollector?.CollectSummary(meta);
            }
            CurrentResponseCollector?.DoneSuccess();
            _logger?.Debug("S: ", new SuccessMessage(meta));
        }

        public void HandleRecordMessage(object[] fields)
        {
            CurrentResponseCollector?.CollectRecord(fields);
            _logger?.Debug("S: ", new RecordMessage(fields));
        }

        public void HandleFailureMessage(string code, string message)
        {
            DequeueMessage();
            Error = ParseServerException(code, message);
            CurrentResponseCollector?.DoneFailure();
            _logger?.Debug("S: ", new FailureMessage(code, message));
        }

        public void HandleIgnoredMessage()
        {
            DequeueMessage();
            CurrentResponseCollector?.DoneIgnored();
            _logger?.Debug("S: ", Ignored);
        }

        public void EnqueueMessage(IRequestMessage requestMessage, IMessageResponseCollector responseCollector = null)
        {
            lock (_syncLock)
            {
                _unhandledMessages.Enqueue(requestMessage);
                _resultBuilders.Enqueue(responseCollector);
            }
        }

        private void DequeueMessage()
        {
            lock (_syncLock)
            {
                _unhandledMessages.Dequeue();
                CurrentResponseCollector = _resultBuilders.Dequeue();
            }
        }
    }
}
