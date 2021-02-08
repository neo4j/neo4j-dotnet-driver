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

		public class ResultConsumeType
		{
			public string resultId { get; set; }
		}

		public override async Task Process()
		{
			await ((SessionResult)ObjManager.GetObject(data.resultId)).ConsumeResults().ConfigureAwait(false);
		}

		public override string Respond()
		{
			return new ProtocolResponse("Summary", (object)null).Encode();
		}
	}
}
