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

using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend;

internal class ResultConsume : IProtocolObject
{
    public ResultConsumeType data { get; set; } = new();

    [JsonIgnore] public IRecord Records { get; set; }

    [JsonIgnore] public IResultSummary Summary { get; set; }

    public override async Task Process()
    {
        Summary = await ((Result)ObjManager.GetObject(data.resultId)).ConsumeResults().ConfigureAwait(false);
    }

    public override string Respond()
    {
        return new ProtocolResponse("Summary", SummaryJsonSerializer.SerializeToRaw(Summary)).Encode();
    }

    public class ResultConsumeType
    {
        public string resultId { get; set; }
    }
}
