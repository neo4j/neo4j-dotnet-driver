using System;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend;

internal class ProtocolException : IProtocolObject
{
    public ProtocolExceptionType data { get; set; } = new();

    [JsonIgnore] public Exception ExceptionObj { get; set; }

    public override async Task Process()
    {
        await Task.CompletedTask;
    }

    public class ProtocolExceptionType
    {
        public string msg { get; set; }
    }
}
