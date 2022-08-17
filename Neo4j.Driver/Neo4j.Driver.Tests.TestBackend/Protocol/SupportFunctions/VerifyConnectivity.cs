using System.Threading.Tasks;

namespace Neo4j.Driver.Tests.TestBackend
{
	internal class VerifyConnectivity : ProtocolObject
	{
		public VerifyConnectivityType Data { get; set; } = new VerifyConnectivityType();

		public class VerifyConnectivityType
		{
			public string driverId { get; set; }
		}

		public override async Task ProcessAsync()
		{
			var driver = ObjManager.GetObject<NewDriver>(Data.driverId).Driver;
			await driver.VerifyConnectivityAsync();			
		}

		public override string Respond()
		{
			return new ProtocolResponse("Driver", UniqueId).Encode();
		}
	}
}
