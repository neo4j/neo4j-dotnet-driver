using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal class SessionBeginTransaction : IProtocolObject
    {
        public SessionBeginTransactionType data { get; set; } = new SessionBeginTransactionType();
        [JsonIgnore]
        public string TransactionId { get; set; }

		public class SessionBeginTransactionType
		{
			public string sessionId { get; set; }

			[JsonProperty(Required = Required.AllowNull)]
			public Dictionary<string, object> txMeta { get; set; } = new Dictionary<string, object>();

			[JsonProperty(Required = Required.AllowNull)]
			public int timeout { get; set; } = -1;
        }

        void TransactionConfig(TransactionConfigBuilder configBuilder)
        {
            if (data.timeout != -1)
            {
                var time = TimeSpan.FromMilliseconds(data.timeout);
                configBuilder.WithTimeout(time);
            }

            if (data.txMeta.Count > 0) configBuilder.WithMetadata(data.txMeta);
        }

		public override async Task Process(Controller controller)
		{
			var sessionContainer = (NewSession)ObjManager.GetObject(data.sessionId);
			var transaction = await sessionContainer.Session.BeginTransactionAsync(TransactionConfig);
			TransactionId = controller.TransactionManagager.AddTransaction(new TransactionWrapper(transaction, async cursor => 
			{	
				var result = ProtocolObjectFactory.CreateObject<SessionResult>();
				result.Results = cursor;

				return await Task.FromResult<string>(result.uniqueId);				
			}));
        }

        public override string Respond()
        {
            return new ProtocolResponse("Transaction", TransactionId).Encode();
        }
    }
}
