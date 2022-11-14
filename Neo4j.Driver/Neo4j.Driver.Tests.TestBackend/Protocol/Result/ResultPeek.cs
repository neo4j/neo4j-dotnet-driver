using System.Linq;
using System.Threading.Tasks;

namespace Neo4j.Driver.Tests.TestBackend;

internal class ResultPeek : IProtocolObject
{
    public ResultPeekType data { get; set; } = new();

    public IRecord Records { get; set; }

    public override async Task Process()
    {
        var result = (Result)ObjManager.GetObject(data.resultId);
        Records = await result.PeekRecord();
    }

    public override string Respond()
    {
        if (Records is null)
        {
            return new ProtocolResponse("NullRecord", (object)null).Encode();
        }

        //Generate list of ordered records
        var valuesList = Records.Keys.Select(v => NativeToCypher.Convert(Records[v]));
        return new ProtocolResponse("Record", new { values = valuesList }).Encode();
    }

    public class ResultPeekType
    {
        public string resultId { get; set; }
    }
}
