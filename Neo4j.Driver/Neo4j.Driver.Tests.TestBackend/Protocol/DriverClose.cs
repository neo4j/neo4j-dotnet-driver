using System;
using System.Threading.Tasks;
using Neo4j.Driver;


namespace Neo4j.Driver.Tests.TestBackend
{ 
    internal class DriverClose : IProtocolObject
    {
        public DriverCloseType data { get; set; } = new DriverCloseType();
        
        public class DriverCloseType
        {
            public string driverId { get; set; }            
        }

        public override async Task Process()
        {
            try
            {
                IDriver driver = ((NewDriver)ObjManager.GetObject(data.driverId)).Driver;
                await driver.CloseAsync();                
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to Process NewDriver protocol object, failed with - {ex.Message}");
            }
        }

        public override string Response()
        {   
            return new Response("Driver", uniqueId).Encode();
        }
    }
}
