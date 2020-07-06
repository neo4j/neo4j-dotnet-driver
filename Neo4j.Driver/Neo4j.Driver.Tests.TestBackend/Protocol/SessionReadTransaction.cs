using System;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using Neo4j.Driver;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal class SessionReadTransaction : IProtocolObject
    {   
        public SessionReadTransactionType data { get; set; } = new SessionReadTransactionType();
        [JsonIgnore]
        public IAsyncTransaction Transaction { get; set; }

        public class SessionReadTransactionType
        {
            public string sessionId { get; set; }            
        }

        public override async Task Process()
        {
            var sessionContainer = (NewSession)ObjManager.GetObject(data.sessionId);
            await sessionContainer.Session.ReadTransactionAsync(async t =>
            {
                Transaction = t;
                await AysncVoidReturn();
            });

            //TODO.... it looks like the go backend  processes further messages to handle a retryable state
            //then returns a RetryableDone in amoungst the other returns....talk to peter.            
        }

        public override string Respond()
        {
            return new Response("RetryableTry", uniqueId).Encode();
        }
    }
}
