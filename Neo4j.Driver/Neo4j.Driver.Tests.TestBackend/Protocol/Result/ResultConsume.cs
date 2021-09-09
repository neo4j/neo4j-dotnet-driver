using System;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend
{
	internal class ResultConsume : IProtocolObject
	{
		public ResultConsumeType data { get; set; } = new ResultConsumeType();
		[JsonIgnore]
		public IRecord Records { get; set; }
		[JsonIgnore]
		public IResultSummary Summary { get; set; }

		public class ResultConsumeType
		{
			public string resultId { get; set; }
		}

		public override async Task Process()
		{
			Summary = await ((Result)ObjManager.GetObject(data.resultId)).ConsumeResults().ConfigureAwait(false);
		}

		public override string Respond()
		{
			return new ProtocolResponse("Summary", new
			{
				serverInfo = new
				{
					protocolVersion = Summary.Server.ProtocolVersion,
					agent = Summary.Server.Agent
				}
			}).Encode();
		}
	}
}
