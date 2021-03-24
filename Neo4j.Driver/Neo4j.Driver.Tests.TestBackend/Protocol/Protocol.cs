using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using System.Threading.Tasks;

namespace Neo4j.Driver.Tests.TestBackend
{
	public class TestKitProtocolException : Exception 
	{
		public TestKitProtocolException(string message) : base(message)
		{

		}
	}

    public static class Protocol
    {
		public static readonly HashSet<Type> ProtocolTypes =
				new HashSet<Type> { typeof(NewDriver),
									typeof(DriverClose),
									typeof(NewSession),
									typeof(SessionClose),
									typeof(AuthorizationToken),
									typeof(SessionRun),
									typeof(TransactionRun),
									typeof(TransactionCommit),
									typeof(TransactionRollback),
									typeof(SessionReadTransaction),
									typeof(SessionWriteTransaction),
									typeof(SessionBeginTransaction),
									typeof(TransactionResult),
									typeof(SessionResult),
									typeof(ResultNext),
									typeof(ResultConsume),
									typeof(RetryablePositive),
									typeof(RetryableNegative),
									typeof(ProtocolException),
									typeof(SessionLastBookmarks),
									typeof(VerifyConnectivity),
									typeof(CheckMultiDBSupport),
									typeof(ResolverResolutionCompleted),
									typeof(StartTest)};

        
		static Protocol()
        {
            
        }

        public static void ValidateType(string typeName) 
        {
			var objectType = Type.GetType(typeName, true);
			ValidateType(objectType);
        }

		public static void ValidateType(Type objectType)
		{
			if (!ProtocolTypes.Contains(objectType)) throw new TestKitProtocolException($"Attempting to use an unrecognized type: {objectType}");
		}
    }


    internal abstract class IProtocolObject
    {
        public string name { get; set; }
        [JsonProperty("id")]
        public string uniqueId { get; internal set; }    //Only exposes the get option so that the serializer will output it.  Don't want to read in on deserialization.
        [JsonIgnore]
        protected ProtocolObjectManager ObjManager { get; set; }        
        public event EventHandler ProtocolEvent;

        public void SetObjectManager(ProtocolObjectManager objManager)
        {
            ObjManager = objManager;
        }

        public void SetUniqueId(string id) { uniqueId = id; }


        public virtual async Task Process()
		{
            await Task.CompletedTask;
        }

        public virtual async Task Process(Controller controller)    //Default is to not use the controller object. But option to override this method and use it if necessary.
		{
            await Process();
        }

        public string Encode()      
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public virtual string Respond()
        {
            return Encode();
        }

        protected void TriggerEvent()
		{
            ProtocolEvent?.Invoke(this, EventArgs.Empty);
        }
    }
}
