using System.Threading.Tasks;

namespace Neo4j.Driver.Tests.TestBackend;

internal class TransactionRollback : IProtocolObject
{
    public TransactionRollbackType data { get; set; } = new();

    public override async Task Process(Controller controller)
    {
        var transactionWrapper = controller.TransactionManager.FindTransaction(data.txId);
        await transactionWrapper.Transaction.RollbackAsync();
    }

    public override string Respond()
    {
        return new ProtocolResponse("Transaction", uniqueId).Encode();
    }

    public class TransactionRollbackType
    {
        public string txId { get; set; }
    }
}
