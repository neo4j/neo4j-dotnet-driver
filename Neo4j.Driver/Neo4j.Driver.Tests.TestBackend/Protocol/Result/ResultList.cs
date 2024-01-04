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
using System.Linq;
using System.Threading.Tasks;
using Neo4j.Driver.Tests.TestBackend.Types;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend.Protocol.Result;

internal class ResultList : ProtocolObject
{
    public ResultListType data { get; set; } = new();

    [JsonIgnore] public List<IRecord> Records { get; set; }

    public override async Task Process()
    {
        var result = (Result)ObjManager.GetObject(data.resultId);
        Records = await result.ToListAsync();
    }

    public override string Respond()
    {
        if (Records == null)
        {
            return new ProtocolResponse("NullRecord", (object)null).Encode();
        }

        var mappedList = Records
            .Select(
                x => new
                {
                    values = x.Values
                        .Select(y => NativeToCypher.Convert(y.Value))
                        .ToList()
                })
            .ToList();

        return new ProtocolResponse("RecordList", new { records = mappedList }).Encode();
    }

    public class ResultListType
    {
        public string resultId { get; set; }
    }
}
