using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Neo4j.Driver;
using System.Collections.Generic;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal class SessionReadTransaction : IProtocolObject
    {   
        public SessionReadTransactionType data { get; set; } = new SessionReadTransactionType();
        [JsonIgnore]
        private string TransactionId { get; set; }
		[JsonIgnore]
		private bool Success { get; set; } = true;

        public class SessionReadTransactionType
        {
            public string sessionId { get; set; }  
            public string cypher { get; set; }

			public int timeout { get; set; } = 0;

			[JsonProperty(Required = Required.AllowNull)]
			public Dictionary<string, object> txMeta { get; set; } = new Dictionary<string, object>();
		}

        public override async Task Process(Controller controller)
        {
			Success = true;

            var sessionContainer = (NewSession)ObjManager.GetObject(data.sessionId);
            await sessionContainer.Session.ReadTransactionAsync(async tx =>
            {
                TransactionId = controller.TransactionManagager.AddTransaction(new TransactionWrapper(tx, async cursor =>
				{
					var result = ProtocolObjectFactory.CreateObject<TransactionResult>();
					await result.PopulateRecords(cursor).ConfigureAwait(false);
					return result.uniqueId;
				}));

				sessionContainer.SessionTransactions.Add(TransactionId);

				await controller.SendResponse(new ProtocolResponse("RetryableTry", TransactionId).Encode()).ConfigureAwait(false);

				try
				{
					//Start another message processing loop to handle the retry mechanism.
					await controller.ProcessStreamObjects().ConfigureAwait(false);
				}
				catch 
				{
					Success = false;
				}

                controller.TransactionManagager.RemoveTransaction(TransactionId);                
            }, TransactionConfig);
        }

        public override string Respond()
        {
			if (Success)
			{
				return new ProtocolResponse("RetryableDone", new { }).Encode();
			}
			else
			{
				return ExceptionManager.GenerateExceptionResponse(new ClientException("Error from client in retryable tx")).Encode();
			}
		}

		void TransactionConfig(TransactionConfigBuilder configBuilder)
		{
			if (data.txMeta.Count > 0) configBuilder.WithMetadata(data.txMeta);

			if (data.timeout > 0) configBuilder.WithTimeout(TimeSpan.FromSeconds(data.timeout));
		}
	}
}
