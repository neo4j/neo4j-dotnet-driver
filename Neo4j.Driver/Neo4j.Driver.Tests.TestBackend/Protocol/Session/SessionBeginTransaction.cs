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

        [JsonConverter(typeof(SessionBeginTransactionTypeJsonConverter))]
		public class SessionBeginTransactionType
		{
			public string sessionId { get; set; }

			[JsonProperty(Required = Required.AllowNull)]
			public Dictionary<string, object> txMeta { get; set; } = new Dictionary<string, object>();

			[JsonProperty(Required = Required.AllowNull)]
            public int? timeout { get; set; }

            [JsonIgnore]
            public bool TimeoutSet { get; set; }
        }

        void TransactionConfig(TransactionConfigBuilder configBuilder)
        {
            try
            {
                if (data.TimeoutSet)
                {
                    var timeout = data.timeout.HasValue
                    ? TimeSpan.FromMilliseconds(data.timeout.Value)
                        : default(TimeSpan?);
                    configBuilder.WithTimeout(timeout);
                }
            }
            catch (ArgumentOutOfRangeException e)
            {
                throw new DriverExceptionWrapper(e);
            }

            if (data.txMeta.Count > 0) configBuilder.WithMetadata(data.txMeta);
        }

		public override async Task Process(Controller controller)
		{
			var sessionContainer = (NewSession)ObjManager.GetObject(data.sessionId);
			var transaction = await sessionContainer.Session.BeginTransactionAsync(TransactionConfig);
			TransactionId = controller.TransactionManager.AddTransaction(new TransactionWrapper(transaction, async cursor =>
			{
				var result = ProtocolObjectFactory.CreateObject<Result>();
				result.ResultCursor = cursor;

				return await Task.FromResult<string>(result.uniqueId);
			}));
        }

        public override string Respond()
        {
            return new ProtocolResponse("Transaction", TransactionId).Encode();
        }
    }
}
