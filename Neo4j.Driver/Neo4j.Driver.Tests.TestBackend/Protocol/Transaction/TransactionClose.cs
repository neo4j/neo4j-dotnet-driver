using System.Threading.Tasks;

namespace Neo4j.Driver.Tests.TestBackend;

internal class TransactionClose : IProtocolObject
{
    public TransactionCloseDataType data { get; set; } = new();

    public override async Task Process(Controller controller)
    {
        var transactionWrapper = controller.TransactionManager.FindTransaction(data.txId);
        await transactionWrapper.Transaction.DisposeAsync();
    }

    public override string Respond()
    {
        return new ProtocolResponse("Transaction", uniqueId).Encode();
    }

    public class TransactionCloseDataType
    {
        public string txId { get; set; }
    }
}
