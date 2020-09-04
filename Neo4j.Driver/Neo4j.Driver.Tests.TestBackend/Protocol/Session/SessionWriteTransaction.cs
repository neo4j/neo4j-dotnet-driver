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
        public string TransactionId { get; set; }

        public class SessionWriteTransactionType
        {
            public string sessionId { get; set; }
        }

        public override async Task Process(Controller controller)
        {
            var sessionContainer = (NewSession)ObjManager.GetObject(data.sessionId);
            await sessionContainer.Session.WriteTransactionAsync(async tx =>
            {
                TransactionId = controller.TransactionManagager.AddTransaction(tx);

                await controller.SendResponse(new ProtocolResponse("RetryableTry", TransactionId).Encode()).ConfigureAwait(false);

                //Start another message processing loop to handle the retry mechanism.
                await controller.ProcessStreamObjects().ConfigureAwait(false);

                controller.TransactionManagager.RemoveTransaction(TransactionId);
            });
        }

        public override string Respond()
        {
            return new ProtocolResponse("RetryableDone", new { }).Encode();
        }
    }
}
