using System;
using System.Threading.Tasks;

namespace Neo4j.Driver.Tests.TestBackend
{
    class TransactionCommit : ProtocolObject
    {
        public TransactionCommitType data { get; set; } = new TransactionCommitType(); 

        public class TransactionCommitType
        {
            public string txId { get; set; }            
        }

        public override async Task ProcessAsync(Controller controller)
        {
            var transactionWrapper = controller.TransactionManager.FindTransaction(data.txId);
            await transactionWrapper.Transaction.CommitAsync();
        }

        public override string Respond()
        {
            return new ProtocolResponse("Transaction", UniqueId).Encode();
        }
    }
}
