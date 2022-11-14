using System.Threading.Tasks;

namespace Neo4j.Driver.Tests.TestBackend;

internal class TransactionCommit : IProtocolObject
{
    public TransactionCommitType data { get; set; } = new();

    public override async Task Process(Controller controller)
    {
        var transactionWrapper = controller.TransactionManager.FindTransaction(data.txId);
        await transactionWrapper.Transaction.CommitAsync();
    }

    public override string Respond()
    {
        return new ProtocolResponse("Transaction", uniqueId).Encode();
    }

    public class TransactionCommitType
    {
        public string txId { get; set; }
    }
}
