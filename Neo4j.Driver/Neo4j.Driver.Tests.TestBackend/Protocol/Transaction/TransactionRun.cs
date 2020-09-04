using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Neo4j.Driver;

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
            var transaction = controller.TransactionManagager.FindTransaction(data.txId);

            IResultCursor cursor = await transaction.RunAsync(data.cypher, data.parameters).ConfigureAwait(false);
                
            var result = new Result() { Results = cursor };
            ObjManager.AddProtocolObject(result);
            ResultId = result.uniqueId;
        }

        public override string Respond()
        {   
            return ((Result)ObjManager.GetObject(ResultId)).Respond();
        }
    }
}
