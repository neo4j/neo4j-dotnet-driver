using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend
{
	internal class SessionWriteTransaction : IProtocolObject
	{
		public SessionWriteTransactionType data { get; set; } = new SessionWriteTransactionType();
		[JsonIgnore]
		public string TransactionId { get; set; }


        public class SessionWriteTransactionType
        {
            public string sessionId { get; set; }

			public int timeout { get; set; } = -1;

			[JsonProperty(Required = Required.AllowNull)]
			public Dictionary<string, object> txMeta { get; set; } = new Dictionary<string, object>();
		}

        public override async Task Process(Controller controller)
        {
            var sessionContainer = (NewSession)ObjManager.GetObject(data.sessionId);

            await sessionContainer.Session.WriteTransactionAsync(async tx =>
            {
				sessionContainer.SetupRetryAbleState(NewSession.SessionState.RetryAbleNothing);

				TransactionId = controller.TransactionManager.AddTransaction(new TransactionWrapper(tx, async cursor =>
				{
					var result = ProtocolObjectFactory.CreateObject<Result>();
					await result.PopulateRecords(cursor).ConfigureAwait(false);
					return result.uniqueId;
				}));

				sessionContainer.SessionTransactions.Add(TransactionId);

				await controller.SendResponse(new ProtocolResponse("RetryableTry", TransactionId).Encode()).ConfigureAwait(false);

				Exception storedException = new TestKitClientException("Error from client");

				while (true)
				{
					try
					{
						//Start another message processing loop to handle the retry mechanism.
						await controller.ProcessStreamObjects().ConfigureAwait(false);
					}
					catch (Exception ex)
					{
						// Generate "driver" exception something happened within the driver
						await controller.SendResponse(ExceptionManager.GenerateExceptionResponse(ex).Encode());
						storedException = ex;
					}

					switch (sessionContainer.RetryState)
					{
						case NewSession.SessionState.RetryAbleNothing:
							break;
						case NewSession.SessionState.RetryAblePositive:
							return;
						case NewSession.SessionState.RetryAbleNegative:
							throw storedException;

						default:
							break;
					}

					//Otherwise keep processing unrelated commands.
				}

                //controller.TransactionManager.RemoveTransaction(TransactionId);
            }, TransactionConfig);
        }

        public override string Respond()
        {
			var sessionContainer = (NewSession)ObjManager.GetObject(data.sessionId);

			if(sessionContainer.RetryState == NewSession.SessionState.RetryAbleNothing)
				throw new ArgumentException("Should never hit this code with a RetryAbleNothing");

			else if(sessionContainer.RetryState == NewSession.SessionState.RetryAbleNegative)
			{
				if (string.IsNullOrEmpty(sessionContainer.RetryableErrorId))
					return ExceptionManager.GenerateExceptionResponse(new ClientException("Error from client in retryable tx")).Encode();
				else
				{
					var exception = ((ProtocolException)(ObjManager.GetObject(sessionContainer.RetryableErrorId))).ExceptionObj;
					return ExceptionManager.GenerateExceptionResponse(exception).Encode();
				}
			}

			return new ProtocolResponse("RetryableDone", new { }).Encode();
		}

		void TransactionConfig(TransactionConfigBuilder configBuilder)
		{
			if (data.txMeta.Count > 0) configBuilder.WithMetadata(data.txMeta);

			if (data.timeout > 0) configBuilder.WithTimeout(TimeSpan.FromSeconds(data.timeout));
		}
	}
}
