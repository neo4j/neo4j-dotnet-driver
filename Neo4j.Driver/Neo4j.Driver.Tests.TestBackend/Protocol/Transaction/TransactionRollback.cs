using System;
using System.Threading.Tasks;

namespace Neo4j.Driver.Tests.TestBackend
{
    class TransactionRollback : ProtocolObject
    {
        public TransactionRollbackType data { get; set; } = new TransactionRollbackType();

        public class TransactionRollbackType
        {
            public string txId { get; set; }
        }

        public override async Task ProcessAsync(Controller controller)
        {
            var transactionWrapper = controller.TransactionManager.FindTransaction(data.txId);
            await transactionWrapper.Transaction.RollbackAsync();
        }

        public override string Respond()
        {
            return new ProtocolResponse("Transaction", UniqueId).Encode();
        }
    }
}
