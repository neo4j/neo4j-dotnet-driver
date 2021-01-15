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

        public override async Task Process()
        {
            //Client failed in some way.
            //Notify any subscribers.
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