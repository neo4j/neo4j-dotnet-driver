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

using System.Linq;
using System.Threading.Tasks;

namespace Neo4j.Driver.Tests.TestBackend;

internal class ResultPeek : ProtocolObject
{
    public ResultPeekType data { get; set; } = new();

    public IRecord Records { get; set; }

    public override async Task Process()
    {
        var result = (Result)ObjManager.GetObject(data.resultId);
        Records = await result.PeekRecord();
    }

    public override string Respond()
    {
        if (Records is null)
        {
            return new ProtocolResponse("NullRecord", (object)null).Encode();
        }

        //Generate list of ordered records
        var valuesList = Records.Keys.Select(v => NativeToCypher.Convert(Records[v]));
        return new ProtocolResponse("Record", new { values = valuesList }).Encode();
    }

    public class ResultPeekType
    {
        public string resultId { get; set; }
    }
}
