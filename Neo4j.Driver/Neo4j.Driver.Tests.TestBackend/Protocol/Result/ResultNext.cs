using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend;

internal class ResultNext : IProtocolObject
{
    public ResultNextType data { get; set; } = new();

    [JsonIgnore] public IRecord Records { get; set; }

    public override async Task Process()
    {
        try
        {
            var result = (Result)ObjManager.GetObject(data.resultId);
            Records = await result.GetNextRecord();
        }
        catch (TimeZoneNotFoundException tz)
        {
            throw new DriverExceptionWrapper(tz);
        }
    }

    public override string Respond()
    {
        if (Records is null)
        {
            return new ProtocolResponse("NullRecord", (object)null).Encode();
        }

        //Generate list of ordered records
        var valuesList = Records.Keys.Select(v => NativeToCypher.Convert(Records[v]));
        try
        {
            return new ProtocolResponse("Record", new { values = valuesList }).Encode();
        }
        catch (TimeZoneNotFoundException tz)
        {
            throw new DriverExceptionWrapper(tz);
        }
    }

    public class ResultNextType
    {
        public string resultId { get; set; }
    }
}
