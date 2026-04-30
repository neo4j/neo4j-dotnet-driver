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
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Result;

namespace Neo4j.Driver.Internal.QueryApi;

internal class QueryApiResultCursor : IResultCursor, IAsyncEnumerator<IRecord>
{
    private readonly IReadOnlyList<IRecord> _records;
    private readonly string[] _keys;
    private readonly Query _query;
    private readonly IResultSummaryFactory _summaryFactory;

    private int _currentIndex = -1;
    private bool _isConsumed;

    public QueryApiResultCursor(
        IReadOnlyList<IRecord> records,
        string[] keys,
        Query query,
        IResultSummaryFactory summaryFactory)
    {
        _records = records;
        _keys = keys;
        _query = query;
        _summaryFactory = summaryFactory;
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

    public Task<bool> FetchAsync() => MoveNextAsync().AsTask();

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
        return Task.FromResult(_summaryFactory.Create(_query));
    }

    public IAsyncEnumerator<IRecord> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new CursorEnumerator(this, cancellationToken);
    }

    public ValueTask DisposeAsync() => default;

    private void AssertNotConsumed()
    {
        if (_isConsumed)
        {
            throw ErrorExtensions.NewResultConsumedException();
        }
    }
}
