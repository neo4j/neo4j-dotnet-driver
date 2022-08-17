
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Routing;
using Newtonsoft.Json;
using System.Linq;

namespace Neo4j.Driver.Tests.TestBackend
{
	class GetRoutingTable : ProtocolObject
	{
		public GetRoutingTableDataType data { get; set; } = new GetRoutingTableDataType();

		[JsonIgnore]
		public IRoutingTable RoutingTable { get; set; }

		public class GetRoutingTableDataType
		{
			public string driverId { get; set; }
			public string database { get; set; }
		}

		public override async Task ProcessAsync(Controller controller)
		{
			var protocolDriver = (NewDriver)ObjManager.GetObject(data.driverId);
			var driver = (Neo4j.Driver.Internal.Driver)protocolDriver.Driver;
			RoutingTable = driver.GetRoutingTable(data.database);

			await Task.CompletedTask;
		}

		public override string Respond()
		{
			return new ProtocolResponse("RoutingTable", new {	database = RoutingTable.Database,
																ttl = "huh",
																routers = RoutingTable.Routers.Select(x => x.Authority),
																readers = RoutingTable.Readers.Select(x => x.Authority),
																writers = RoutingTable.Writers.Select(x => x.Authority)
															}).Encode();
		}
	}
}
