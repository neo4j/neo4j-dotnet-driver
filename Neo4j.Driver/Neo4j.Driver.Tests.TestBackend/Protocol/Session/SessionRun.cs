using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal class SessionRun : IProtocolObject
    {
        public SessionRunType data { get; set; } = new SessionRunType();

        [JsonIgnore]
        private string ResultId { get; set; }

        public class SessionRunType
        {
            public string sessionId { get; set; }

            public string cypher { get; set; }

            [JsonProperty("params")]
            public Dictionary<string, CypherToNativeObject> parameters { get; set; } = new Dictionary<string, CypherToNativeObject>();

            [JsonProperty(Required = Required.AllowNull)]
            public Dictionary<string, object> txMeta { get; set; } = new Dictionary<string, object>();

            public int? timeout { get; set; }
        }

        private Dictionary<string, object> ConvertParameters(Dictionary<string, CypherToNativeObject> source)
        {
            if (data.parameters == null)
                return null;

            Dictionary<string, object> newParams = new Dictionary<string, object>();

            foreach(KeyValuePair<string, CypherToNativeObject> element in source)
            {
                newParams.Add(element.Key, CypherToNative.Convert(element.Value));
            }

            return newParams;
        }

        void TransactionConfig(TransactionConfigBuilder configBuilder)
        {
            if (data.timeout.HasValue)
            {
                configBuilder.WithTimeout(TimeSpan.FromMilliseconds(data.timeout.Value));
            }

            if (data.txMeta.Count > 0) configBuilder.WithMetadata(data.txMeta);
        }

        public override async Task Process()
        {
            var newSession = (NewSession)ObjManager.GetObject(data.sessionId);
            IResultCursor cursor = await newSession.Session.RunAsync(data.cypher, ConvertParameters(data.parameters), TransactionConfig).ConfigureAwait(false);

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
