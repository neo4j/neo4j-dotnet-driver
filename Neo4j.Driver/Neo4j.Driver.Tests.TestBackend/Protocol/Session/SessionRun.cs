// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend;

internal class SessionRun : ProtocolObject
{
    public SessionRunType data { get; set; } = new();

    [JsonIgnore] private string ResultId { get; set; }

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

    [JsonConverter(typeof(SessionTypeJsonConverter))]
    public class SessionRunType : BaseSessionType
    {
        public string cypher { get; set; }

        [JsonProperty("params")]
        [JsonConverter(typeof(QueryParameterConverter))]
        public Dictionary<string, CypherToNativeObject> parameters { get; set; } = new();
    }
}
