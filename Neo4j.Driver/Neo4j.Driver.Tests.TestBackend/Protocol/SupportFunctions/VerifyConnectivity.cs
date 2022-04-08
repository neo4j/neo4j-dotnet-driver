using System.Threading.Tasks;

namespace Neo4j.Driver.Tests.TestBackend
{
	internal class VerifyConnectivity : IProtocolObject
	{
		public VerifyConnectivityType Data { get; set; } = new VerifyConnectivityType();

		public class VerifyConnectivityType
		{
			public string DriverId { get; set; }
		}

		public override async Task Process()
		{
			var driver = ObjManager.GetObject<NewDriver>(Data.DriverId).Driver;
			await driver.VerifyConnectivityAsync();			
		}

		public override string Respond()
		{
			return new ProtocolResponse("Driver", uniqueId).Encode();
		}
	}
}
