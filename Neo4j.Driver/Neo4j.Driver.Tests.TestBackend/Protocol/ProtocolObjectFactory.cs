using System.Text.Json;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal class ProtocolObjectFactory
    {
        
        private ProtocolObjectManager ObjManager { get; set; }

        public ProtocolObjectFactory(ProtocolObjectManager manager)
        {
            ObjManager = manager;
        }

        public IProtocolObject CreateObject(Protocol.Types objectType, string jsonString = null)
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
                case Protocol.Types.Result:
                    //not used in requests so should not be produced by factory.
                    break;
                case Protocol.Types.SessionReadTransaction:
                    newObject = string.IsNullOrEmpty(jsonString) ? new SessionReadTransaction() : JsonSerializer.Deserialize<SessionReadTransaction>(jsonString);
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
            }

            if (newObject is null)
                throw new System.Exception("Trying to create a none supported object in the ProtocolObjectFactory");

            newObject.SetObjectManager(ObjManager);
            ObjManager.AddProtocolObject(newObject);
            return newObject;
        }
    }
}
