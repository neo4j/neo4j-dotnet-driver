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
        public IAsyncTransaction Transaction { get; set; }

        public class SessionWriteTransactionType
        {
            public string sessionId { get; set; }
        }

        public override async Task Process()
        {
            var sessionContainer = (NewSession)ObjManager.GetObject(data.sessionId);
            await sessionContainer.Session.WriteTransactionAsync(async t =>
            {
                Transaction = t;
                await AysncVoidReturn();
            });
        }

        public override string Respond()
        {
            return new ProtocolResponse("RetryableTry", uniqueId).Encode();
        }
    }
}
