using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Neo4j.Driver;

namespace Neo4j.Driver.Tests.TestBackend
{
	internal class SessionWriteTransaction : IProtocolObject
	{
		public SessionWriteTransactionType data { get; set; } = new SessionWriteTransactionType();
		[JsonIgnore]
		public string TransactionId { get; set; }
		[JsonIgnore]
		private bool Success { get; set; } = true;

        public class SessionWriteTransactionType
        {
            public string sessionId { get; set; }
        }

        public override async Task Process(Controller controller)
        {
			Success = true;

            var sessionContainer = (NewSession)ObjManager.GetObject(data.sessionId);
            await sessionContainer.Session.WriteTransactionAsync(async tx =>
            {
                TransactionId = controller.TransactionManagager.AddTransaction(new TransactionWrapper(tx, async cursor => 
				{
					var result = (TransactionResult)ProtocolObjectFactory.CreateObject(Protocol.Types.TransactionResult);
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
            });
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
    }
}
