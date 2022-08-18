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

using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend;

internal class Result : ProtocolObject
{
    [JsonIgnore] public IResultCursor ResultCursor { get; set; }

    public ResultType data { get; set; } = new();

    public override string Respond()
    {
        var keys = ResultCursor.KeysAsync().GetAwaiter().GetResult();
        return new ProtocolResponse("Result", new {id = UniqueId, keys}).Encode();
    }

    public async Task<IRecord> GetNextRecordAsync()
    {
        if (await ResultCursor.FetchAsync())
            return ResultCursor.Current;

        return null;
    }

    public Task<IRecord> PeekRecordAsync()
    {
        return ResultCursor.PeekAsync();
    }

    public Task<IRecord> SingleAsync()
    {
        return ResultCursor.SingleAsync();
    }

    public Task<IResultSummary> ConsumeAsync()
    {
        return ResultCursor.ConsumeAsync();
    }

    public Task<List<IRecord>> ToListAsync()
    {
        return ResultCursor.ToListAsync();
    }

    public class ResultType
    {
        public string id { get; set; }
    }
}