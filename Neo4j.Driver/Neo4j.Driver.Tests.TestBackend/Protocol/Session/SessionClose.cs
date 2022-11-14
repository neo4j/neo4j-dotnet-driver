using System.Threading.Tasks;

namespace Neo4j.Driver.Tests.TestBackend;

internal class SessionClose : IProtocolObject
{
    public SessionCloseType data { get; set; } = new();

    public override async Task Process()
    {
        var session = ((NewSession)ObjManager.GetObject(data.sessionId)).Session;
        await session.CloseAsync();
    }

    public override string Respond()
    {
        return new ProtocolResponse("Session", uniqueId).Encode();
    }

    public class SessionCloseType
    {
        public string sessionId { get; set; }
    }
}
