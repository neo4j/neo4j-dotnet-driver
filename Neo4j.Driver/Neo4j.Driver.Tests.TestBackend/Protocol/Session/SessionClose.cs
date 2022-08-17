using System;
using System.Threading.Tasks;
using Neo4j.Driver;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal class SessionClose : ProtocolObject
    {
        public SessionCloseType data { get; set; } = new SessionCloseType();
       
        public class SessionCloseType
        {
            public string sessionId { get; set; }
        }

        public override async Task ProcessAsync()
        {   
            IAsyncSession session = ((NewSession)ObjManager.GetObject(data.sessionId)).Session;
            await session.CloseAsync();
        }

        public override string Respond()
        {  
            return new ProtocolResponse("Session", UniqueId).Encode();
        }
    }
}
