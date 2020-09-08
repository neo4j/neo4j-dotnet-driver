using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using Neo4j.Driver;

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
            
        }

        private Dictionary<string, object> ConvertParameters()
		{
            if (data.parameters == null)
                return null;

            Dictionary<string, object> newParams = new Dictionary<string, object>();

            foreach(KeyValuePair<string, CypherToNativeObject> element in data.parameters)
			{
                newParams.Add(element.Key, CypherToNative.Convert(element.Value));
			}

            return newParams;
		}

        public override async Task Process()
        {
            var newSession = (NewSession)ObjManager.GetObject(data.sessionId);

            IResultCursor cursor = await newSession.Session.RunAsync(data.cypher, ConvertParameters()).ConfigureAwait(false);

            var result = (Result)ProtocolObjectFactory.CreateObject(Protocol.Types.Result);
            result.Results = cursor;
            
            ResultId = result.uniqueId;
        }

        public override string Respond()
        {   
            return ((Result)ObjManager.GetObject(ResultId)).Respond();
        }
    }
}
