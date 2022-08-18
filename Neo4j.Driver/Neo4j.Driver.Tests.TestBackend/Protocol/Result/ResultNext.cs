// Copyright (c) 2002-2022 "Neo4j,"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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

internal class ResultNext : ProtocolObject
{
    public ResultNextType data { get; set; } = new();
    [JsonIgnore] public IRecord Records { get; set; }

    public override async Task ProcessAsync()
    {
        try
        {
            var result = ObjManager.GetObject<Result>(data.resultId);
            Records = await result.GetNextRecordAsync();
        }
        catch (TimeZoneNotFoundException tz)
        {
            throw new DriverExceptionWrapper(tz);
        }
    }

    public override string Respond()
    {
        if (Records is null) return new ProtocolResponse("NullRecord", (object) null).Encode();

        //Generate list of ordered records
        var valuesList = Records.Keys.Select(v => NativeToCypher.Convert(Records[v]));
        try
        {
            return new ProtocolResponse("Record", new {values = valuesList}).Encode();
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