using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend
{
	class CheckMultiDBSupport : IProtocolObject
	{
		public CheckMultiDBSupportType data { get; set; } = new CheckMultiDBSupportType();

		[JsonIgnore]
		private bool MutlitDBSupportAvailable { get; set; }

		public class CheckMultiDBSupportType
		{
			public string driverId { get; set; }
		}

		public override async Task Process()
		{
			var driver = ((NewDriver)ObjManager.GetObject(data.driverId)).Driver;
			MutlitDBSupportAvailable = await driver.SupportsMultiDbAsync();
		}

		public override string Respond()
		{
			return new ProtocolResponse("MultiDBSupport", new { id = uniqueId, available = MutlitDBSupportAvailable }).Encode();
		}
	}
}
