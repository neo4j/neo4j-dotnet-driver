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
            //Get the session
			var sessionContainer = ((NewSession)ObjManager.GetObject(data.sessionId));

			//rollback any transactions in progress
			sessionContainer.SessionTransactions.ForEach(async t =>
			{
				await controller.
						TransactionManagager.
						FindTransaction(t).
						Transaction.
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