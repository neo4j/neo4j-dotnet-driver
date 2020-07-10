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

        public override async Task Process()
        {
            var transaction = ((SessionReadTransaction)ObjManager.GetObject(data.txId)).Transaction;
            await transaction.RollbackAsync();
        }

        public override string Respond()
        {
            return new ProtocolResponse("Transaction", uniqueId).Encode();
        }
    }
}
