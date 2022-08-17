using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Linq;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal class ResultPeek : ProtocolObject
    {
        public ResultPeekType data { get; set; } = new ResultPeekType();

        public IRecord Records { get; set; }

        public class ResultPeekType
        {
            public string resultId { get; set; }
        }

        public override async Task ProcessAsync()
        {
            var result = (Result)ObjManager.GetObject(data.resultId);
            Records = await result.PeekRecord();
        }

        public override string Respond()
        {
            if (Records is null)
                return new ProtocolResponse("NullRecord", (object)null).Encode();

            //Generate list of ordered records
            var valuesList = Records.Keys.Select(v => NativeToCypher.Convert(Records[v]));
            return new ProtocolResponse("Record", new { values = valuesList }).Encode();
        }
    }
}
