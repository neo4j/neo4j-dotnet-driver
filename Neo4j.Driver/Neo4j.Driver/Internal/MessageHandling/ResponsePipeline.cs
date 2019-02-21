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
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Messaging;

namespace Neo4j.Driver.Internal.MessageHandling
{
    internal class ResponsePipeline : IResponsePipeline
    {
        private const string MessagePattern = "S: {0}";

        private readonly LinkedList<IResponseHandler> _handlers;
        private readonly IDriverLogger _logger;

        private int _unHandledMessages;
        private IResponsePipelineError _error;

        public ResponsePipeline(IDriverLogger logger)
        {
            _handlers = new LinkedList<IResponseHandler>();
            _logger = logger;
            _unHandledMessages = 0;
            _error = null;
        }

        internal IResponseHandler Current =>
            _handlers.First?.Value ?? throw new InvalidOperationException("Handlers is empty.");

        public bool HasNoPendingMessages => _unHandledMessages == 0;

        public void Enqueue(IRequestMessage message, IResponseHandler handler)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            _handlers.AddLast(handler ?? throw new ArgumentNullException(nameof(handler)));
            _unHandledMessages++;
        }

        public void AssertNoFailure()
        {
            _error?.EnsureThrown();
        }

        public void AssertNoProtocolViolation()
        {
            _error?.EnsureThrownIf<ProtocolException>();
        }

        public IResponseHandler Dequeue()
        {
            var first = _handlers.First?.Value;
            _handlers.RemoveFirst();
            _unHandledMessages--;
            return first;
        }

        public async Task OnSuccessAsync(IDictionary<string, object> metadata)
        {
            LogSuccess(metadata);
            var currentHandler = Current;
            Dequeue();
            await currentHandler.OnSuccessAsync(metadata);
        }

        public async Task OnRecordAsync(object[] fieldValues)
        {
            LogRecord(fieldValues);
            await Current.OnRecordAsync(fieldValues);
        }

        public async Task OnFailureAsync(string code, string message)
        {
            LogFailure(code, message);
            var currentHandler = Current;
            Dequeue();

            _error = new ResponsePipelineError(ErrorExtensions.ParseServerException(code, message));

            await currentHandler.OnFailureAsync(_error);
        }

        public async Task OnIgnoredAsync()
        {
            LogIgnored();
            var currentHandler = Current;
            Dequeue();

            if (_error != null)
            {
                await currentHandler.OnFailureAsync(_error);
            }
            else
            {
                await currentHandler.OnIgnoredAsync();
            }
        }

        private void LogSuccess(IDictionary<string, object> meta)
        {
            if (_logger != null && _logger.IsDebugEnabled())
            {
                _logger?.Debug(MessagePattern, new SuccessMessage(meta));
            }
        }

        private void LogRecord(object[] fields)
        {
            if (_logger != null && _logger.IsDebugEnabled())
            {
                _logger?.Debug(MessagePattern, new RecordMessage(fields));
            }
        }

        private void LogFailure(string code, string message)
        {
            if (_logger != null && _logger.IsDebugEnabled())
            {
                _logger?.Debug(MessagePattern, new FailureMessage(code, message));
            }
        }

        private void LogIgnored()
        {
            if (_logger != null && _logger.IsDebugEnabled())
            {
                _logger?.Debug(MessagePattern, IgnoredMessage.Ignored);
            }
        }
    }
}