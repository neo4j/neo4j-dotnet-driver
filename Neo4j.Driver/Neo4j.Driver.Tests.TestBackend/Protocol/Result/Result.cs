using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
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
            await AsyncVoidReturn();
        }

        public override string Respond()
        {
            return new ProtocolResponse("Result", uniqueId).Encode();
        }
    }
}
