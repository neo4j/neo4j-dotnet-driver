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

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Result;

namespace Neo4j.Driver.Internal.QueryApi;

internal class QueryApiResultCursor : IResultCursor, IAsyncEnumerator<IRecord>
{
    private readonly List<IRecord> _records;
    private readonly string[] _keys;
    private readonly Query _query;
    private readonly IServerInfo _serverInfo;
    private readonly string _database;

    private int _currentIndex = -1;
    private bool _isConsumed;

    public QueryApiResultCursor(
        QueryApiResponse response,
        Query query,
        IServerInfo serverInfo,
        string database)
    {
        _query = query;
        _serverInfo = serverInfo;
        _database = database;
        _keys = response.Fields;

        var lookup = new Dictionary<string, int>(response.Fields.Length, StringComparer.Ordinal);
        var invariantLookup = new Dictionary<string, int>(response.Fields.Length, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < response.Fields.Length; i++)
        {
            lookup[response.Fields[i]] = i;
            invariantLookup[response.Fields[i]] = i;
        }

        _records = response.Rows
            .Select(IRecord (row) => new Record(lookup, invariantLookup, row.Select(ConvertElement).ToArray()!))
            .ToList();
    }

    IRecord IResultCursor.Current
    {
        get
        {
            AssertNotConsumed();
            return _currentIndex >= 0 
                ? _records[_currentIndex] 
                : throw new InvalidOperationException("Tried to access Current without calling FetchAsync.");
        }
    }

    IRecord IAsyncEnumerator<IRecord>.Current => _records[_currentIndex];

    public bool IsOpen => !_isConsumed;

    public Task<string[]> KeysAsync() => Task.FromResult(_keys);

    public Task<bool> FetchAsync()
    {
        return MoveNextAsync().AsTask();
    }

    public ValueTask<bool> MoveNextAsync()
    {
        AssertNotConsumed();
        var nextIndex = _currentIndex + 1;
        if (nextIndex >= _records.Count)
        {
            return new ValueTask<bool>(false);
        }

        _currentIndex = nextIndex;
        return new ValueTask<bool>(true);

    }

    Task<IRecord> IResultCursor.PeekAsync()
    {
        AssertNotConsumed();

        var nextIndex = _currentIndex + 1;
        return nextIndex < _records.Count 
            ? Task.FromResult(_records[nextIndex]) 
            : Task.FromResult<IRecord>(null!);
    }

    public Task<IResultSummary> ConsumeAsync()
    {
        _isConsumed = true;
        return Task.FromResult<IResultSummary>(new ResultSummary(_query, _serverInfo, _database));
    }

    public IAsyncEnumerator<IRecord> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new CursorEnumerator(this, cancellationToken);
    }

    public ValueTask DisposeAsync() => default;

    private static object? ConvertElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var l) ? (object)l : element.GetDouble(),
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertElement).ToList(),
            JsonValueKind.Object => ConvertObject(element),
            _ => throw new ArgumentOutOfRangeException(nameof(element), element.ValueKind, "Unexpected JSON value kind.")
        };
    }

    private static object? ConvertObject(JsonElement element)
    {
        if (element.TryGetProperty("$type", out var typeElement))
        {
            var typeName = typeElement.GetString() ?? "unknown";
            Trace.TraceWarning($"[QueryApiResultCursor] Unsupported Neo4j typed value: {typeName}");
            return $"Unsupported type: {typeName}";
        }

        var dict = new Dictionary<string, object?>();
        foreach (var prop in element.EnumerateObject())
        {
            dict[prop.Name] = ConvertElement(prop.Value);
        }

        return dict;
    }

    private void AssertNotConsumed()
    {
        if (_isConsumed)
        {
            throw ErrorExtensions.NewResultConsumedException();
        }
    }

    private sealed class ResultSummary : IResultSummary
    {
        public ResultSummary(Query query, IServerInfo serverInfo, string database)
        {
            Query = query;
            Server = serverInfo;
            Database = new DatabaseInfo(database);
            Counters = new Counters();
        }

        public Query Query { get; }
        public ICounters Counters { get; }
        public QueryType QueryType => QueryType.Unknown;
        public bool HasPlan => false;
        public bool HasProfile => false;
        public IPlan? Plan => null;
        public IProfiledPlan? Profile => null;

#pragma warning disable CS0618
        public IList<INotification>? Notifications => null;
#pragma warning restore CS0618

        public IList<IGqlStatusObject>? GqlStatusObjects => null;
        public TimeSpan ResultAvailableAfter => TimeSpan.FromMilliseconds(-1);
        public TimeSpan ResultConsumedAfter => TimeSpan.FromMilliseconds(-1);
        public IServerInfo Server { get; }
        public IDatabaseInfo Database { get; }
    }
}
