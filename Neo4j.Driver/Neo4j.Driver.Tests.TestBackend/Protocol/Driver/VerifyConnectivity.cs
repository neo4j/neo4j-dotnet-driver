using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo4j.Driver.Tests.TestBackend
{
	internal class VerifyConnectivity : IProtocolObject
	{
		public VerifyConnectivityType data { get; set; } = new VerifyConnectivityType();

		public class VerifyConnectivityType
		{
			public string driverId { get; set; }
		}

		public override async Task Process()
		{
			IDriver driver = ((NewDriver)ObjManager.GetObject(data.driverId)).Driver;
			await driver.VerifyConnectivityAsync();			
		}

		public override string Respond()
		{
			return new ProtocolResponse("Driver", uniqueId).Encode();
		}
	}
}
