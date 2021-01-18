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
        }

        public override async Task Process(Controller controller)
        {
			Success = true;

            var sessionContainer = (NewSession)ObjManager.GetObject(data.sessionId);
            await sessionContainer.Session.ReadTransactionAsync(async tx =>
            {
                TransactionId = controller.TransactionManagager.AddTransaction(tx);

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
