using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend;

internal class ResolverResolutionCompleted : IProtocolObject
{
    public ResolverResolutionCompletedType data { get; set; } = new();

    [JsonIgnore] public ListAddressResolver Resolver { get; private set; }

    public override async Task Process()
    {
        await Task.CompletedTask;
    }

    public class ResolverResolutionCompletedType
    {
        public string requestId { get; set; }
        public List<string> addresses { get; set; } = new();
    }
}
