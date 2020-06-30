using System;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using Neo4j.Driver;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal class Result : IProtocolObject
    {
        public ResultType data { get; set; } = new ResultType();
        [JsonIgnore]
        public IResultCursor Results { get; set; }


        public class ResultType
        {
            public string id { get; set; }
        }



        public override async Task Process()
        {
            try
            {
                //Currently does nothing
                await AysncVoidReturn();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to Process TransactionRun protocol object, failed with - {ex.Message}");
            }
        }

        public override string Response()
        {
            return new Response("Result", uniqueId).Encode();
        }
    }
}
