using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal class RetryableNegative : ProtocolObject
    {
        public RetryableNegativeType data { get; set; } = new RetryableNegativeType();

        public class RetryableNegativeType
        {
            public string sessionId { get; set; }
            public string errorId { get; set; }
        }

        public override async Task ProcessAsync(Controller controller)
        {
			var sessionContainer = ((NewSession)ObjManager.GetObject(data.sessionId));
			sessionContainer.SetupRetryAbleState(NewSession.SessionState.RetryAbleNegative, data.errorId);

			TriggerEvent();

			await Task.CompletedTask;
		}


        public override string Respond()
        {
			return string.Empty;
        }
    }
}