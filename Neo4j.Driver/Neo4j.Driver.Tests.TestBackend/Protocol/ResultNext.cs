using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Neo4j.Driver;
using System.Linq;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal class ResultNext : IProtocolObject
    {
        public ResultNextType data { get; set; } = new ResultNextType();
        [JsonIgnore]
        public IRecord Records { get; set; }

        public class ResultNextType
        {
            public string resultId { get; set; }
        }

        public override async Task Process()
        {
            try
            {

                var results = ((Result)ObjManager.GetObject(data.resultId)).Results;

                if (await results.FetchAsync().ConfigureAwait(false))
                    Records = results.Current;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to Process NewDriver protocol object, failed with - {ex.Message}");
            }
        }

        public override string Response()
        {
            //var values = Records.Values.Values.Select(v => NativeToCypher.InternalConvert(v));
            //return new Response("Record", new { values = values }).Encode();
            var translatedResults = NativeToCypher.Convert(Records.Values);
            return new Response("Record", translatedResults).Encode();
        }
    }
}
