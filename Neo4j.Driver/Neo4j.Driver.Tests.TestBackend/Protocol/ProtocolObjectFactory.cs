using System.Text.Json;
using System;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal static class ProtocolObjectFactory
    {
        
        public static ProtocolObjectManager ObjManager { get; set; }
       

        public static IProtocolObject CreateObject(Protocol.Types objectType, string jsonString = null)
        {
            IProtocolObject newObject = null;
            switch (objectType)
            {
                case Protocol.Types.NewDriver:
                    newObject = string.IsNullOrEmpty(jsonString) ? new NewDriver() : JsonSerializer.Deserialize<NewDriver>(jsonString);
                    break;
                case Protocol.Types.NewSession:
                    newObject = string.IsNullOrEmpty(jsonString) ? new NewSession() : JsonSerializer.Deserialize<NewSession>(jsonString);
                    break;
                case Protocol.Types.AuthorizationToken:
                    newObject = string.IsNullOrEmpty(jsonString) ? new AuthorizationToken() : JsonSerializer.Deserialize<AuthorizationToken>(jsonString);
                    break;
                case Protocol.Types.SessionRun:
                    newObject = string.IsNullOrEmpty(jsonString) ? new SessionRun() : JsonSerializer.Deserialize<SessionRun>(jsonString);
                    break;                    
                case Protocol.Types.TransactionRun:
                    newObject = string.IsNullOrEmpty(jsonString) ? new TransactionRun() : JsonSerializer.Deserialize<TransactionRun>(jsonString);
                    break;
                case Protocol.Types.TransactionCommit:
                    newObject = string.IsNullOrEmpty(jsonString) ? new TransactionCommit() : JsonSerializer.Deserialize<TransactionCommit>(jsonString);
                    break;
                case Protocol.Types.Result:
                    newObject = string.IsNullOrEmpty(jsonString) ? new Result() : JsonSerializer.Deserialize<Result>(jsonString);
                    break;
                case Protocol.Types.SessionReadTransaction:
                    newObject = string.IsNullOrEmpty(jsonString) ? new SessionReadTransaction() : JsonSerializer.Deserialize<SessionReadTransaction>(jsonString);
                    break;
                case Protocol.Types.SessionWriteTransaction:
                    newObject = string.IsNullOrEmpty(jsonString) ? new SessionWriteTransaction() : JsonSerializer.Deserialize<SessionWriteTransaction>(jsonString);
                    break;
                case Protocol.Types.SessionBeginTransaction:
                    newObject = string.IsNullOrEmpty(jsonString) ? new SessionBeginTransaction() : JsonSerializer.Deserialize<SessionBeginTransaction>(jsonString);
                    break;
                case Protocol.Types.DriverClose:
                    newObject = string.IsNullOrEmpty(jsonString) ? new DriverClose() : JsonSerializer.Deserialize<DriverClose>(jsonString);
                    break;
                case Protocol.Types.SessionClose:
                    newObject = string.IsNullOrEmpty(jsonString) ? new SessionClose() : JsonSerializer.Deserialize<SessionClose>(jsonString);
                    break;
                case Protocol.Types.ResultNext:
                    newObject = string.IsNullOrEmpty(jsonString) ? new ResultNext() : JsonSerializer.Deserialize<ResultNext>(jsonString);
                    break;
                case Protocol.Types.RetryablePositive:
                    newObject = string.IsNullOrEmpty(jsonString) ? new RetryablePositive() : JsonSerializer.Deserialize<RetryablePositive>(jsonString);
                    break;
                case Protocol.Types.RetryableNegative:
                    newObject = string.IsNullOrEmpty(jsonString) ? new RetryableNegative() : JsonSerializer.Deserialize<RetryableNegative>(jsonString);
                    break;
                case Protocol.Types.ProtocolException:
                    newObject = string.IsNullOrEmpty(jsonString) ? new ProtocolException() : JsonSerializer.Deserialize<ProtocolException>(jsonString);
                    break;
            }

            if (newObject is null)
                throw new Exception($"Trying to create a none supported object in the ProtocolObjectFactory of type {objectType}");

            newObject.SetObjectManager(ObjManager);
            ObjManager.AddProtocolObject(newObject);
            return newObject;
        }
    }
}
