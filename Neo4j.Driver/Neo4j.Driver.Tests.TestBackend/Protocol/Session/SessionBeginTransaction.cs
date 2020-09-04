using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Neo4j.Driver;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal class SessionBeginTransaction : IProtocolObject
    {
        public SessionBeginTransactionType data { get; set; } = new SessionBeginTransactionType();
        [JsonIgnore]
        public string TransactionId { get; set; }

        public class SessionBeginTransactionType
        {
            public string sessionId { get; set; }
        }

        public override async Task Process(Controller controller)
        {
            var sessionContainer = (NewSession)ObjManager.GetObject(data.sessionId);
            var transaction = await sessionContainer.Session.BeginTransactionAsync();
            TransactionId = controller.TransactionManagager.AddTransaction(transaction);
        }

        public override string Respond()
        {
            return new ProtocolResponse("Transaction", TransactionId).Encode();
        }
    }
}
