using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Neo4j.Driver;
using System.Linq;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal class TransactionRun : IProtocolObject
    {
        public TransactionRunType data { get; set; } = new TransactionRunType();
        [JsonIgnore]
        private string ResultId { get; set; }

        public class TransactionRunType
        {
            public string txId { get; set; }
            public string cypher { get; set; }
            [JsonProperty("params")]
            public Dictionary<string, object> parameters { get; set; } = new Dictionary<string, object>();
        }

        public override async Task Process(Controller controller)
        {
            var transactionWrapper = controller.TransactionManagager.FindTransaction(data.txId);

            IResultCursor cursor = await transactionWrapper.Transaction.RunAsync(data.cypher, data.parameters).ConfigureAwait(false);

			ResultId = await transactionWrapper.ProcessResults(cursor);
		}

        public override string Respond()
        {   
            return ((Result)ObjManager.GetObject(ResultId)).Respond();
        }
    }
}
