using System.Threading.Tasks;

namespace Neo4j.Driver.Tests.TestBackend;

internal class VerifyConnectivity : IProtocolObject
{
    public VerifyConnectivityType Data { get; set; } = new();

    public override async Task Process()
    {
        var driver = ObjManager.GetObject<NewDriver>(Data.driverId).Driver;
        await driver.VerifyConnectivityAsync();
    }

    public override string Respond()
    {
        return new ProtocolResponse("Driver", uniqueId).Encode();
    }

    public class VerifyConnectivityType
    {
        public string driverId { get; set; }
    }
}
