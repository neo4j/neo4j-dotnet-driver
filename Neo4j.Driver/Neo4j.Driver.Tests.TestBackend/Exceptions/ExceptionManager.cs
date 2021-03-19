using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Neo4j.Driver.Tests.TestBackend
{
    //TransientException = DriverError
    //ClientException = ClientError
    //All others = BackendError


    /*
    neo4jException
    |  ClientException
    |  |   ValueTruncationException
    |  |   ValueOverflowException
    |  |   FatalDiscoveryException
    |  |   ResultConsumedException
    |  TransientException
    |  DatabaseException
    |  ServiceUnavailableException
    |  SessionExpiredException
    |  ProtocolException
    |  SecurityException
    |  |   AuthenticationException
    */

    internal static class ExceptionManager
    {
        private static Dictionary<Type, string> TypeMap { get; set; } = new Dictionary<Type, string>()
        {
            { typeof(Neo4jException),                   "Neo4jError" },
            { typeof(ClientException),                  "ClientError" },
            { typeof(TransientException),               "DriverError" },        //Should maybe Transient error, talk to Peter or Martin
            { typeof(DatabaseException),                "DatabaseError" },
            { typeof(ServiceUnavailableException),      "ServiceUnavailableError" },
            { typeof(SessionExpiredException),          "SessionExpiredError" },
            { typeof(ProtocolException),                "ProtocolError" },
            { typeof(SecurityException),                "SecurityError" },
            { typeof(AuthenticationException),          "AuthenticationError" },
            { typeof(ValueTruncationException),         "ValueTruncationError" },
            { typeof(ValueOverflowException),           "ValueOverflowError" },
            { typeof(FatalDiscoveryException),          "FatalDiscoveryError" },
            { typeof(ResultConsumedException),          "ResultConsumedError" },

            { typeof(NotSupportedException),            "NotSupportedException" }
        };


        internal static ProtocolResponse GenerateExceptionResponse(Exception ex)
        {
            string exceptionMessage = (ex.InnerException != null) ? ex.InnerException.Message : ex.Message;

            if (ex is Neo4jException || ex is NotSupportedException) {
                ProtocolException newError = ProtocolObjectFactory.CreateObject<ProtocolException>();
                newError.ExceptionObj = ex;
                return new ProtocolResponse("DriverError", new { id = newError.uniqueId, errorType = TypeMap[ex.GetType()], msg = exceptionMessage } );
            }
            Trace.WriteLine($"Exception thrown {exceptionMessage}\n{ex.StackTrace}");
            return new ProtocolResponse("BackendError", new { msg = exceptionMessage } );
        }
    }    
}
