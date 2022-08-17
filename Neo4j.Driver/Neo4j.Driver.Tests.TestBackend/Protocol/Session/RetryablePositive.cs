using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal class RetryablePositive : ProtocolObject
    {
        public RetryablePositiveType data { get; set; } = new RetryablePositiveType();

        public class RetryablePositiveType
        {
            public string sessionId { get; set; }
        }

        public override async Task ProcessAsync()
        {
			var sessionContainer = ((NewSession)ObjManager.GetObject(data.sessionId));
			sessionContainer.SetupRetryAbleState(NewSession.SessionState.RetryAblePositive);

			//Client succeded and wants to commit. 
			//Notify any subscribers.

			TriggerEvent();			

			await Task.CompletedTask;
        }

        public override string Respond()
        {
            return string.Empty;            
        }
    }
}
