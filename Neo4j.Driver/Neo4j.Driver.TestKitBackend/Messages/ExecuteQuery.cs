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

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Cypher;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.ObjectRegistry;
using Neo4j.Driver.TestKitBackend.Serialization;
using Neo4j.Driver.TestKitBackend.Summary;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal record ExecuteQueryConfig
{
    public string? Routing { get; init; }
    public string? Database { get; init; }
    public string? ImpersonatedUser { get; init; }

    // testkit sends the disable sentinel as the JSON number -1, and a real id as a JSON string.
    [JsonConverter(typeof(BookmarkManagerIdConverter))]
    public string? BookmarkManagerId { get; init; }

    public Dictionary<string, ICypherValue>? TxMeta { get; init; }
    public long? Timeout { get; init; }
    public IWireType<AuthorizationToken>? AuthorizationToken { get; init; }
}

internal class BookmarkManagerIdConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType == JsonTokenType.Number
            ? reader.GetInt64().ToString(CultureInfo.InvariantCulture)
            : reader.GetString();
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}

internal record ExecuteQueryRequest : IProtocolMessage
{
    public required RegistryObject<IDriver> Driver { get; init; }
    public required string Cypher { get; init; }
    public Dictionary<string, ICypherValue>? Params { get; init; }
    public required ExecuteQueryConfig Config { get; init; }
}

internal record EagerResultResponse(
    string[] Keys,
    IReadOnlyList<RecordResponse> Records,
    SummaryResponse Summary) : IProtocolMessage;

internal class ExecuteQueryHandler : MessageHandler<ExecuteQueryRequest>
{
    private readonly ICypherToNativeMapper _cypherToNativeMapper;
    private readonly INativeToCypherMapper _nativeToCypherMapper;
    private readonly ISummaryMapper _summaryMapper;
    private readonly IExecuteQueryConfigMapper _configMapper;
    private readonly IResponseWriter _responseWriter;
    private readonly ILogger _logger;

    public ExecuteQueryHandler(
        ICypherToNativeMapper cypherToNativeMapper,
        INativeToCypherMapper nativeToCypherMapper,
        ISummaryMapper summaryMapper,
        IExecuteQueryConfigMapper configMapper,
        IResponseWriter responseWriter,
        ILogger logger)
    {
        _cypherToNativeMapper = cypherToNativeMapper;
        _nativeToCypherMapper = nativeToCypherMapper;
        _summaryMapper = summaryMapper;
        _configMapper = configMapper;
        _responseWriter = responseWriter;
        _logger = logger;
    }

    public override async Task ProcessAsync(ExecuteQueryRequest message)
    {
        _logger.LogDebug(
            "Executing query '{Cypher}' on driver with id '{DriverId}'",
            message.Cypher,
            message.Driver.Id);

        var eagerResult = await message.Driver.Object
            .ExecutableQuery(message.Cypher)
            .WithParameters(_cypherToNativeMapper.Map(message.Params))
            .WithConfig(_configMapper.Map(message.Config))
            .ExecuteAsync();

        var records = eagerResult.Result
            .Select(record =>
                new RecordResponse(record.Keys.Select(key => _nativeToCypherMapper.Map(record[key])).ToList()))
            .ToList();

        await _responseWriter.WriteAsync(
            new EagerResultResponse(eagerResult.Keys, records, _summaryMapper.Map(eagerResult.Summary)));
    }
}
