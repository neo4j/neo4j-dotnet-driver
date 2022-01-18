using System.Collections.Generic;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal abstract class BaseSessionType
    {
        public string sessionId { get; set; }

        [JsonProperty(Required = Required.AllowNull)]
        public Dictionary<string, object> txMeta { get; set; } = new Dictionary<string, object>();

        [JsonProperty(Required = Required.AllowNull)]
        public int? timeout { get; set; }

        [JsonIgnore]
        public bool TimeoutSet { get; set; }
    }
}