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
			string responseName = "RunTest";
			string reason = string.Empty;
			if (TestBlackList.FindTest(data.testName, out reason)) responseName = "SkipTest";
			
			return new ProtocolResponse(responseName, reason).Encode();
		}
	}
}
