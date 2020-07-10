using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Neo4j.Driver.Tests.TestBackend
{
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
            { typeof(ResultConsumedException),          "ResultConsumedError" }
        };
             

        internal static ProtocolResponse GenerateExceptionResponse(Exception ex)
        {   
            string exceptionName;
            string exceptionMessage = (ex.InnerException != null) ? ex.InnerException.Message : ex.Message;

            if (!TypeMap.ContainsKey(ex.GetType()))
                exceptionName = "BackendError";             //This handles all system exceptions.
            else
                exceptionName = TypeMap[ex.GetType()];

            ProtocolException newError = (ProtocolException)ProtocolObjectFactory.CreateObject(Protocol.Types.ProtocolException);
            newError.ExceptionObj = ex;

            Trace.WriteLine($"Exception thrown {exceptionMessage}\n{ex.StackTrace}");

            return new ProtocolResponse(exceptionName, new { id = newError.uniqueId, msg = exceptionMessage } );
        }
    }    
}
