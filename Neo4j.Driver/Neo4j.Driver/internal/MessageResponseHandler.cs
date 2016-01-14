using System.Collections.Generic;
using Neo4j.Driver.Internal.messaging;
using Neo4j.Driver.Internal.result;

namespace Neo4j.Driver
{
    internal class MessageResponseHandler : IMessageResponseHandler
    {
        private readonly Queue<ResultBuilder> _resultBuilders = new Queue<ResultBuilder>();
        private readonly Queue<IMessage> _messages = new Queue<IMessage>() ;
        private ResultBuilder _currentResultBuilder;

        public void HandleSuccessMessage(IDictionary<string, object> meta)
        {
            var message = _messages.Dequeue();
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

        public void HandleRecordMessage()
        {
//            _currentResultBuilder.AddRecord();
        }

        public void Register(IMessage message, ResultBuilder resultBuilder = null)
        {
            _messages.Enqueue(message);
            if (resultBuilder != null)
            {
                _resultBuilders.Enqueue(resultBuilder);
            }
        }

        public bool QueueIsEmpty()
        {
            return _messages.Count == 0;
        }
    }
}