using System;
using System.Threading.Tasks;

namespace Neo4j.Driver.Tests.TestBackend
{
    class TransactionCommit : IProtocolObject
    {
        public TransactionCommitType data { get; set; } = new TransactionCommitType(); 

        public class TransactionCommitType
        {
            public string txId { get; set; }            
        }

        public override async Task Process()
        {
            var transaction = ((SessionBeginTransaction)ObjManager.GetObject(data.txId)).Transaction;
            await transaction.CommitAsync();
        }

        public override string Respond()
        {
            return new ProtocolResponse("Transaction", uniqueId).Encode();
        }
    }
}
