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
        private readonly Queue<IMessage> _sentMessages = new Queue<IMessage>() ;
        private ResultBuilder _currentResultBuilder;

        public void HandleSuccessMessage(IDictionary<string, object> meta)
        {
            var message = _sentMessages.Dequeue();
            if (message is InitMessage)
            {
                return;
                // do nothing
            }

            // suc for run 
            // deq and save

            // suc for pull all
            // deq and save
            _currentResultBuilder = _resultBuilders.Dequeue();
            _currentResultBuilder.CollectMeta(meta);
        }

        public void HandleFailureMessage(string code, string message)
        {

            throw new System.NotImplementedException();
        }

        public void HandleIgnoredMessage()
        {
            throw new System.NotImplementedException();
        }

        public void Register(IMessage message, ResultBuilder resultBuilder = null)
        {
            _sentMessages.Enqueue(message);
            if (resultBuilder != null)
            {
                _resultBuilders.Enqueue(resultBuilder);
            }
        }

        public bool QueueIsEmpty()
        {
            return _sentMessages.Count == 0;
        }

        public void HandleRecordMessage(dynamic[] fields)
        {
            //TODO: Should error if no keys??
            //var message = _sentMessages.Dequeue();
//            if (!(message is PullAllMessage))
//            {
//                Throw.ArgumentException.IfNotEqual(message.GetType(), typeof(PullAllMessage), "Dequeued Messages", "Expected Messages");
//            }

            //var builder = _resultBuilders.Dequeue();
            //Throw.ArgumentException.IfNotEqual( builder, _currentResultBuilder, "Dequeued builder", "Expected builder");
            _currentResultBuilder.Record( fields);
        }
    }
}