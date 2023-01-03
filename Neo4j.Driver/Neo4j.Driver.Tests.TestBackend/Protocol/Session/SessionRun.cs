using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend
{
	internal class SessionRun : IProtocolObject
    {
        public SessionRunType data { get; set; } = new SessionRunType();

        [JsonIgnore]
        private string ResultId { get; set; }

        [JsonConverter(typeof(SessionTypeJsonConverter))]
        public class SessionRunType : BaseSessionType
        {
            public string cypher { get; set; }

            [JsonProperty("params")]
            [JsonConverter(typeof(QueryParameterConverter))]
            public Dictionary<string, CypherToNativeObject> parameters { get; set; } = new Dictionary<string, CypherToNativeObject>();
        }

        public override async Task Process()
        {
            var newSession = (NewSession)ObjManager.GetObject(data.sessionId);
            var cursor = await newSession.Session
                .RunAsync(
                    data.cypher,
                    CypherToNativeObject.ConvertDictionaryToNative(data.parameters),
                    data.TransactionConfig)
                .ConfigureAwait(false);

            var result = ProtocolObjectFactory.CreateObject<Result>();
            result.ResultCursor = cursor;

            ResultId = result.uniqueId;
        }

        public override string Respond()
        {
            return ((Result)ObjManager.GetObject(ResultId)).Respond();
        }
    }
}
