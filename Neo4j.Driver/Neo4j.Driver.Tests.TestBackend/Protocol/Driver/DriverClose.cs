using System.Threading.Tasks;

namespace Neo4j.Driver.Tests.TestBackend;

internal class DriverClose : IProtocolObject
{
    public DriverCloseType data { get; set; } = new();

    public override async Task Process()
    {
        var driver = ((NewDriver)ObjManager.GetObject(data.driverId)).Driver;
        await driver.CloseAsync();
    }

    public override string Respond()
    {
        return new ProtocolResponse("Driver", uniqueId).Encode();
    }

    public class DriverCloseType
    {
        public string driverId { get; set; }
    }
}
