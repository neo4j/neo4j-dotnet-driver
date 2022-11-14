using System.Collections.Generic;
using System.Threading.Tasks;

namespace Neo4j.Driver.Tests.TestBackend;

internal class AuthorizationToken : IProtocolObject
{
    public AuthorizationTokenType data { get; set; } = new();

    public override Task Process()
    {
        return Task.CompletedTask;
    }

    public class AuthorizationTokenType
    {
        public string scheme { get; set; }
        public string principal { get; set; }
        public string credentials { get; set; }
        public string realm { get; set; }
        public Dictionary<string, object> parameters { get; set; }
    }
}
