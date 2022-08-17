using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend
{
	class ResolverResolutionCompleted : ProtocolObject
	{
		public ResolverResolutionCompletedType data { get; set; } = new ResolverResolutionCompletedType();
		[JsonIgnore]
		public ListAddressResolver Resolver { get; private set; }

		public class ResolverResolutionCompletedType
		{
			public string requestId { get; set; }
			public List<string> addresses { get; set; } = new List<string>();
		}

		public override async Task ProcessAsync()
		{	
			await Task.CompletedTask;
		}
	}
}
