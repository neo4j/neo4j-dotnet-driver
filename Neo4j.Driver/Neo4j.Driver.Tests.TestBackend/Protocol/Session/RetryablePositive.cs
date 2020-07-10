using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

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
            await AysncVoidReturn();
        }

        public override string Respond()
        {
            return new ProtocolResponse("RetryableDone", new { }).Encode();
        }
    }
}
