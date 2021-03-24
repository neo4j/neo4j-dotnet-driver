using System;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend
{
	class StartTest : IProtocolObject
	{
		public StartTestType data { get; set; } = new StartTestType();
		
		public class StartTestType
		{
			public string testName { get; set; }
		}

		public override async Task Process()
		{	
			await Task.CompletedTask;
		}

		public override string Respond()
		{	
			string reason = string.Empty;
			if (TestBlackList.FindTest(data.testName, out reason))			
				return new ProtocolResponse("SkipTest", new { reason }).Encode();
			else
				return new ProtocolResponse("RunTest").Encode();
		}
	}
}
