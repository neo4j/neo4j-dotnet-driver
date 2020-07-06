using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Neo4j.Driver.Tests.TestBackend
{
    public static class ExceptionExtensions
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
            return new Response(TypeMap[ex.GetType()], new { msg = ex.Message } );
        }
    }    
}
