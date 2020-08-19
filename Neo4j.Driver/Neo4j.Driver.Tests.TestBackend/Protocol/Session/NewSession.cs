using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using Neo4j.Driver;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal  class NewSession : IProtocolObject
    {
        public NewSessionType data { get; set; } = new NewSessionType();
        [JsonIgnore]
        public IAsyncSession Session { get; set; }

        public class NewSessionType
        {
            public string driverId { get; set; }
            public string accessMode { get; set; }
            public string bookmarks { get; set; }
        }

        public override async Task Process()
        {
            IDriver driver = ((NewDriver)ObjManager.GetObject(data.driverId)).Driver;
            Session = driver.AsyncSession();    //TODO: Use config builder to take into account bookmarks and accessmode.
            await AysncVoidReturn();
        }

        public override string Respond()
        {  
            return new ProtocolResponse("Session", uniqueId).Encode();
        }
    }
}
