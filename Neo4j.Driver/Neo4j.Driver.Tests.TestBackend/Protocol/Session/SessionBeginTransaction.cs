using System;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using Neo4j.Driver;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal class SessionBeginTransaction : IProtocolObject
    {
        public SessionBeginTransactionType data { get; set; } = new SessionBeginTransactionType();
        [JsonIgnore]
        public IAsyncTransaction Transaction { get; set; }

        public class SessionBeginTransactionType
        {
            public string sessionId { get; set; }
        }

        public override async Task Process()
        {
            var sessionContainer = (NewSession)ObjManager.GetObject(data.sessionId);
            Transaction = await sessionContainer.Session.BeginTransactionAsync();
        }

        public override string Respond()
        {
            return new Response("Transaction", uniqueId).Encode();
        }
    }
}
