
namespace Neo4j.Driver.Tests.TestBackend;

class StartTest : ProtocolObject
{
    public StartTestType data { get; set; } = new StartTestType();
		
    public class StartTestType
    {
        public string testName { get; set; }
    }

    public override string Respond()
    {
        return TestBlackList.FindTest(data.testName, out var reason) 
            ? new ProtocolResponse("SkipTest", new { reason }).Encode() 
            : new ProtocolResponse("RunTest").Encode();
    }
}