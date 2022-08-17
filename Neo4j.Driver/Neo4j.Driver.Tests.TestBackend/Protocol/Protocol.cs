using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo4j.Driver.Tests.TestBackend
{
    public class TestKitProtocolException : Exception
    {
        public TestKitProtocolException(string message) : base(message)
        {
        }
    }

    public class TestKitClientException : Exception
    {
        public TestKitClientException(string message) : base(message)
        {
        }
    }


    public static class Protocol
    {
        public static readonly HashSet<Type> ProtocolTypes =
            new HashSet<Type>
            {
                typeof(NewDriver),
                typeof(DriverClose),
                typeof(NewSession),
                typeof(SessionClose),
                typeof(AuthorizationToken),
                typeof(SessionRun),
                typeof(TransactionRun),
                typeof(TransactionCommit),
                typeof(TransactionRollback),
                typeof(TransactionClose),
                typeof(SessionReadTransaction),
                typeof(SessionWriteTransaction),
                typeof(SessionBeginTransaction),
                typeof(Result),
                typeof(ResultNext),
                typeof(ResultPeek),
                typeof(ResultList),
                typeof(ResultSingle),
                typeof(ResultConsume),
                typeof(RetryablePositive),
                typeof(RetryableNegative),
                typeof(ProtocolException),
                typeof(SessionLastBookmarks),
                typeof(VerifyConnectivity),
                typeof(GetServerInfo),
                typeof(CheckMultiDBSupport),
                typeof(CheckDriverIsEncrypted),
                typeof(ResolverResolutionCompleted),
                typeof(StartTest),
                typeof(GetFeatures),
                typeof(GetRoutingTable),
                typeof(CypherTypeField)
            };


        static Protocol()
        {
        }

        public static void ValidateType(string typeName)
        {
            try
            {
                var objectType = Type.GetType(typeof(Protocol).Namespace + "." + typeName, true);
                ValidateType(objectType);
            }
            catch
            {
                throw new TestKitProtocolException($"Attempting to use an unrecognized protocol type: {typeName}");
            }
        }

        public static void ValidateType(Type objectType)
        {
            if (!ProtocolTypes.Contains(objectType))
                throw new TestKitProtocolException($"Attempting to use an unrecognized protocol type: {objectType}");
        }
    }
}