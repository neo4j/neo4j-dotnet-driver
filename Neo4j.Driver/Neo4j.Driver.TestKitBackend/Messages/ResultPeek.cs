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

using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Cypher;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Serialization;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal record ResultPeekRequest : IProtocolMessage
{
    [StoredObject]
    public required IResultCursor Result { get; init; }
}

internal class ResultPeekHandler : MessageHandler<ResultPeekRequest>
{
    private readonly INativeToCypherMapper _mapper;
    private readonly IResponseWriter _responseWriter;

    public ResultPeekHandler(INativeToCypherMapper mapper, IResponseWriter responseWriter)
    {
        _mapper = mapper;
        _responseWriter = responseWriter;
    }

    public override async Task ProcessAsync(ResultPeekRequest message)
    {
        var record = await message.Result.PeekAsync();
        if (record is null)
        {
            await _responseWriter.WriteAsync(new NullRecordResponse());
            return;
        }

        var values = record.Keys.Select(key => _mapper.Map(record[key])).ToList();
        await _responseWriter.WriteAsync(new RecordResponse(values));
    }
}
