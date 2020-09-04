using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal class RetryablePositive : IProtocolObject
    {
        public RetryablePositiveType data { get; set; } = new RetryablePositiveType();

        public class RetryablePositiveType
        {
            public string sessionId { get; set; }
        }

        public override async Task Process()
        {
            //Client succeded and wants to commit. 
            //Notify any subscribers.
            TriggerEvent();

            await AsyncVoidReturn();
        }

        public override string Respond()
        {
            return string.Empty;            
        }
    }
}
