using System;
using Newtonsoft.Json;

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
                    newObject = string.IsNullOrEmpty(jsonString) ? new NewDriver() : JsonConvert.DeserializeObject<NewDriver>(jsonString);
                    break;
                case Protocol.Types.DriverClose:
                    newObject = string.IsNullOrEmpty(jsonString) ? new DriverClose() : JsonConvert.DeserializeObject<DriverClose>(jsonString);
                    break;
                case Protocol.Types.NewSession:
                    newObject = string.IsNullOrEmpty(jsonString) ? new NewSession() : JsonConvert.DeserializeObject<NewSession>(jsonString, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                    break;
                case Protocol.Types.SessionClose:
                    newObject = string.IsNullOrEmpty(jsonString) ? new SessionClose() : JsonConvert.DeserializeObject<SessionClose>(jsonString);
                    break;
                case Protocol.Types.AuthorizationToken:
                    newObject = string.IsNullOrEmpty(jsonString) ? new AuthorizationToken() : JsonConvert.DeserializeObject<AuthorizationToken>(jsonString);
                    break;
                case Protocol.Types.SessionRun:
                    newObject = string.IsNullOrEmpty(jsonString) ? new SessionRun() : JsonConvert.DeserializeObject<SessionRun>(jsonString, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                    break;                    
                case Protocol.Types.TransactionRun:
                    newObject = string.IsNullOrEmpty(jsonString) ? new TransactionRun() : JsonConvert.DeserializeObject<TransactionRun>(jsonString);
                    break;
                case Protocol.Types.TransactionCommit:
                    newObject = string.IsNullOrEmpty(jsonString) ? new TransactionCommit() : JsonConvert.DeserializeObject<TransactionCommit>(jsonString);
                    break;
                case Protocol.Types.TransactionRollback:
                    newObject = string.IsNullOrEmpty(jsonString) ? new TransactionRollback() : JsonConvert.DeserializeObject<TransactionRollback>(jsonString);
                    break;                
                case Protocol.Types.SessionReadTransaction:
                    newObject = string.IsNullOrEmpty(jsonString) ? new SessionReadTransaction() : JsonConvert.DeserializeObject<SessionReadTransaction>(jsonString);
                    break;
                case Protocol.Types.SessionWriteTransaction:
                    newObject = string.IsNullOrEmpty(jsonString) ? new SessionWriteTransaction() : JsonConvert.DeserializeObject<SessionWriteTransaction>(jsonString);
                    break;
                case Protocol.Types.SessionBeginTransaction:
                    newObject = string.IsNullOrEmpty(jsonString) ? new SessionBeginTransaction() : JsonConvert.DeserializeObject<SessionBeginTransaction>(jsonString, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                    break;
                case Protocol.Types.Result:
                    newObject = string.IsNullOrEmpty(jsonString) ? new Result() : JsonConvert.DeserializeObject<Result>(jsonString);
                    break;
                case Protocol.Types.ResultNext:
                    newObject = string.IsNullOrEmpty(jsonString) ? new ResultNext() : JsonConvert.DeserializeObject<ResultNext>(jsonString);
                    break;
                case Protocol.Types.RetryablePositive:
                    newObject = string.IsNullOrEmpty(jsonString) ? new RetryablePositive() : JsonConvert.DeserializeObject<RetryablePositive>(jsonString);
                    break;
                case Protocol.Types.RetryableNegative:
                    newObject = string.IsNullOrEmpty(jsonString) ? new RetryableNegative() : JsonConvert.DeserializeObject<RetryableNegative>(jsonString);
                    break;
                case Protocol.Types.ProtocolException:
                    newObject = string.IsNullOrEmpty(jsonString) ? new ProtocolException() : JsonConvert.DeserializeObject<ProtocolException>(jsonString);
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
