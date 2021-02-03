using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal class RetryableNegative : IProtocolObject
    {
        public RetryableNegativeType data { get; set; } = new RetryableNegativeType();

        public class RetryableNegativeType
        {
            public string sessionId { get; set; }
            public string errorId { get; set; }
        }

        public override async Task Process(Controller controller)
        {
            //Client failed in some way.
            //Notify any subscribers.
			
			//Get the session
			//rollback any transactions in progress
			var sessionContainer = ((NewSession)ObjManager.GetObject(data.sessionId));

			sessionContainer.SessionTransactions.ForEach(async t =>
			{
				await controller.
						TransactionManagager.
						FindTransaction(t).
						RollbackAsync().
						ConfigureAwait(false);
			});


			TriggerException();

            await Task.CompletedTask;
        }

        public override string Respond()
        {
			return string.Empty;
        }

		private void TriggerException()
		{
			Exception ex = null;
			if (string.IsNullOrEmpty(data.errorId))
			{
				ex = new ClientException("Error from client in retryable tx");
			}
			else
			{
				ex = ((ProtocolException)ObjManager.GetObject(data.errorId)).ExceptionObj;
			}

			throw ex;
		}
    }
}