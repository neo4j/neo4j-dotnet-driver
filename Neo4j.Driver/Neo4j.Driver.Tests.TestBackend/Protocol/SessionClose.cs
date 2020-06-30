using System;
using System.Threading.Tasks;
using Neo4j.Driver;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal class SessionClose : IProtocolObject
    {
        public SessionCloseType data { get; set; } = new SessionCloseType();
       
        public class SessionCloseType
        {
            public string sessionId { get; set; }
        }

        public override async Task Process()
        {
            try
            {
                IAsyncSession session = ((NewSession)ObjManager.GetObject(data.sessionId)).Session;
                await session.CloseAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to Process NewDriver protocol object, failed with - {ex.Message}");
            }
        }

        public override string Response()
        {  
            return new Response("Session", uniqueId).Encode();
        }
    }
}
