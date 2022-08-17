using System.Threading.Tasks;

namespace Neo4j.Driver.Tests.TestBackend
{ 
    internal class DriverClose : ProtocolObject
    {
        public DriverCloseType data { get; set; } = new DriverCloseType();
        
        public class DriverCloseType
        {
            public string driverId { get; set; }
        }

        public override async Task ProcessAsync()
        {
            IDriver driver = ((NewDriver)ObjManager.GetObject(data.driverId)).Driver;
            await driver.CloseAsync();                            
        }

        public override string Respond()
        {   
            return new ProtocolResponse("Driver", UniqueId).Encode();
        }
    }
}
