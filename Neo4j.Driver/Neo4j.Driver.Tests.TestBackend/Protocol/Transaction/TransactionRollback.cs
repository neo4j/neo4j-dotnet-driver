using System;
using System.Threading.Tasks;

namespace Neo4j.Driver.Tests.TestBackend
{
    class TransactionRollback : IProtocolObject
    {
        public TransactionRollbackType data { get; set; } = new TransactionRollbackType();

        public class TransactionRollbackType
        {
            public string txId { get; set; }
        }

        public override async Task Process(Controller controller)
        {
            var transaction = controller.TransactionManagager.FindTransaction(data.txId);
            await transaction.RollbackAsync();
        }

        public override string Respond()
        {
            return new ProtocolResponse("Transaction", uniqueId).Encode();
        }
    }
}
