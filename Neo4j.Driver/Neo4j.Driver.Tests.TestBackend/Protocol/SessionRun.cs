using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
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
            [JsonPropertyName("params")]
            public Dictionary<string, object> parameters { get; set; } = new Dictionary<string, object>();
        }

        public override async Task Process()
        {
            try
            {
                var newSession = (NewSession)ObjManager.GetObject(data.sessionId);

                IResultCursor cursor = await newSession.Session.RunAsync(data.cypher, data.parameters).ConfigureAwait(false);
                
                var result = new Result() { Results = cursor };
                ObjManager.AddProtocolObject(result);
                ResultId = result.uniqueId;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to Process SessionRun protocol object, failed with - {ex.Message}");
            }

        }

        public override string Response()
        {   
            return ((Result)ObjManager.GetObject(ResultId)).Response();
        }
    }
}
