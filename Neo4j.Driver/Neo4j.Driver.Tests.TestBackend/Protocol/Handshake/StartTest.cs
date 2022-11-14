using System.Threading.Tasks;

namespace Neo4j.Driver.Tests.TestBackend;

internal class StartTest : IProtocolObject
{
    public StartTestType data { get; set; } = new();

    public override async Task Process()
    {
        await Task.CompletedTask;
    }

    public override string Respond()
    {
        var reason = string.Empty;
        if (TestBlackList.FindTest(data.testName, out reason))
        {
            return new ProtocolResponse("SkipTest", new { reason }).Encode();
        }

        return new ProtocolResponse("RunTest").Encode();
    }

    public class StartTestType
    {
        public string testName { get; set; }
    }
}
