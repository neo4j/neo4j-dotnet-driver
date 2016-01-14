using System.Collections;
using System.Collections.Generic;
using Neo4j.Driver.Internal.result;

namespace Neo4j.Driver
{
    public interface IMessageRequestHandler
    {
        void HandleInitMessage(string clientNameAndVersion);

        void HandleRunMessage(string statement, IDictionary<string, object> parameters);

        void HandlePullAllMessage();

        /*void HandleDiscardAllMessage();

        void HandleAckFailureMessage();

        // Responses
        void HandleSuccessMessage(IDictionary<string, object> meta);

        void HandleRecordMessage(object[] fields);

        void HandleFailureMessage(string code, string message);

        void HandleIgnoredMessage();*/
    }

    public interface IMessageResponseHandler
    {
        void HandleSuccessMessage(IDictionary<string, object> meta);

//        void HandleRecordMessage(object[] fields);
//
//        void HandleFailureMessage(string code, string message);
//
//        void HandleIgnoredMessage();
//        void RegisterResultBuilder(ResultBuilder resultBuilder);
//        void RegisterMessage(IMessage message);
        void Register(IMessage message, ResultBuilder resultBuilder = null);
        bool QueueIsEmpty();
    }
}