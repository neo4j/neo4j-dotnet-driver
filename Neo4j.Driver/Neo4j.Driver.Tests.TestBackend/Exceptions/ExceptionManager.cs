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
        internal static ProtocolResponse GenerateExceptionResponse(Exception ex)
        {
            string exceptionMessage = (ex.InnerException != null) ? ex.InnerException.Message : ex.Message;

            if (ex is Neo4jException || ex is NotSupportedException) {
                ProtocolException newError = (ProtocolException)ProtocolObjectFactory.CreateObject(Protocol.Types.ProtocolException);
                newError.ExceptionObj = ex;
                return new ProtocolResponse("DriverError", new { id = newError.uniqueId, msg = exceptionMessage } );
            }
            Trace.WriteLine($"Exception thrown {exceptionMessage}\n{ex.StackTrace}");
            return new ProtocolResponse("BackendError", new { msg = exceptionMessage } );
        }
    }    
}
