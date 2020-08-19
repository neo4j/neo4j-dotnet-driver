
using System.Threading.Tasks;


namespace Neo4j.Driver.Tests.TestBackend
{
    internal class AuthorizationToken : IProtocolObject
    {
        public AuthorizationTokenType data { get; set; } = new AuthorizationTokenType();

        public class AuthorizationTokenType
        {
            public string scheme { get; set; }
            public string principal { get; set; }
            public string credentials { get; set; }
            public string realm { get; set; }
            public string ticket { get; set; }
        }

        public override async Task Process()
        {
            await Task.Run(() => { });
        }
    }
}
