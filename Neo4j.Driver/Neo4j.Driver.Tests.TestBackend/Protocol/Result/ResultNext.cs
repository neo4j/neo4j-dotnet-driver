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

using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend;

internal class ResultNext : IProtocolObject
{
    public ResultNextType data { get; set; } = new();

    [JsonIgnore] public IRecord Records { get; set; }

    public override async Task Process()
    {
        try
        {
            var result = (Result)ObjManager.GetObject(data.resultId);
            Records = await result.GetNextRecord();
        }
        catch (TimeZoneNotFoundException tz)
        {
            throw new DriverExceptionWrapper(tz);
        }
    }

    public override string Respond()
    {
        if (Records is null)
        {
            return new ProtocolResponse("NullRecord", (object)null).Encode();
        }

        //Generate list of ordered records
        var valuesList = Records.Keys.Select(v => NativeToCypher.Convert(Records[v]));
        try
        {
            return new ProtocolResponse("Record", new { values = valuesList }).Encode();
        }
        catch (TimeZoneNotFoundException tz)
        {
            throw new DriverExceptionWrapper(tz);
        }
    }

    public class ResultNextType
    {
        public string resultId { get; set; }
    }
}
