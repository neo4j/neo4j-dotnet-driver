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

internal class Result : ProtocolObject
{
    [JsonIgnore] public IResultCursor ResultCursor { get; set; }

    public ResultType data { get; set; } = new();

    public override async Task Process()
    {
        //Currently does nothing
        await Task.CompletedTask;
    }

    public override string Respond()
    {
        var keys = ResultCursor.KeysAsync().GetAwaiter().GetResult();
        return new ProtocolResponse("Result", new { id = uniqueId, keys }).Encode();
    }

    public async Task<IRecord> GetNextRecord()
    {
        if (await ResultCursor.FetchAsync())
        {
            return await Task.FromResult(ResultCursor.Current);
        }

        return await Task.FromResult<IRecord>(null);
    }

    public Task<IRecord> PeekRecord()
    {
        return ResultCursor.PeekAsync();
    }

    public Task<IRecord> SingleAsync()
    {
        return ResultCursor.SingleAsync();
    }

    public async Task<IResultSummary> ConsumeResults()
    {
        return await ResultCursor.ConsumeAsync().ConfigureAwait(false);
    }

    public Task<List<IRecord>> ToListAsync()
    {
        return ResultCursor.ToListAsync();
    }

    public async Task PopulateRecords(IResultCursor cursor)
    {
        ResultCursor = cursor;
        await Task.CompletedTask;
    }

    public class ResultType
    {
        public string id { get; set; }
    }
}
