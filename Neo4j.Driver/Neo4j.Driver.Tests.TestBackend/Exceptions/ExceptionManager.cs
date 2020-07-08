using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal static class ExceptionManager
    {
        private static Dictionary<Type, string> TypeMap { get; set; } = new Dictionary<Type, string>()
        {
            { typeof(Exception),                        "BackendError" },
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
             

        internal static Response GenerateExceptionResponse(Exception ex)
        {
            ProtocolException newError = (ProtocolException)ProtocolObjectFactory.CreateObject(Protocol.Types.ProtocolException);
            newError.ExceptionObj = ex;

            return new Response(TypeMap[ex.GetType()], new { id = newError.uniqueId, msg = ex.Message } );
        }
    }    
}
