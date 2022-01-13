using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal class SessionReadTransaction : IProtocolObject
    {
        public SessionReadTransactionType data { get; set; } = new SessionReadTransactionType();
        [JsonIgnore]
        private string TransactionId { get; set; }


        [JsonConverter(typeof(SessionReadTransactionTypeJsonConverter))]
        public class SessionReadTransactionType
        {
            public string sessionId { get; set; }

            public string cypher { get; set; }

            public int? timeout { get; set; }

            [JsonIgnore]
            public bool TimeoutSet { get; set; }

			[JsonProperty(Required = Required.AllowNull)]
			public Dictionary<string, object> txMeta { get; set; } = new Dictionary<string, object>();
		}

        public override async Task Process(Controller controller)
        {
            var sessionContainer = (NewSession)ObjManager.GetObject(data.sessionId);

            await sessionContainer.Session.ReadTransactionAsync(async tx =>
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

				await controller.Process(false, e =>
				{
					switch (sessionContainer.RetryState)
					{
						case NewSession.SessionState.RetryAbleNothing:
							return true;
						case NewSession.SessionState.RetryAblePositive:
							return false;
						case NewSession.SessionState.RetryAbleNegative:
							throw e;

						default:
							return true;
					}
				});

			}, TransactionConfig);
        }

        public override string Respond()
        {
			var sessionContainer = (NewSession)ObjManager.GetObject(data.sessionId);

			if (sessionContainer.RetryState == NewSession.SessionState.RetryAbleNothing)
				throw new ArgumentException("Should never hit this code with a RetryAbleNothing");

			else if (sessionContainer.RetryState == NewSession.SessionState.RetryAbleNegative)
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
            catch (ArgumentOutOfRangeException e) when ((data.timeout ?? 0) < 0 && e.ParamName == "value")
            {
                throw new DriverExceptionWrapper(e);
            }
		}
	}
}
