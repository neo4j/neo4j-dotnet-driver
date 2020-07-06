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
            //Currently does nothing
            await AysncVoidReturn();
        }

        public override string Respond()
        {
            return new Response("Result", uniqueId).Encode();
        }
    }
}
