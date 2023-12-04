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
using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal.Result;

internal sealed class ConsumableResultCursor : IInternalResultCursor, IAsyncEnumerator<IRecord>
{
    private readonly IInternalResultCursor _cursor;
    private bool _isConsumed;
    private readonly IAsyncEnumerator<IRecord> _enumeator;

    public ConsumableResultCursor(IInternalResultCursor cursor)
    {
        _cursor = cursor;
        _enumeator = _cursor.GetAsyncEnumerator();
    }

    public ValueTask<bool> MoveNextAsync()
    {
        AssertNotConsumed();
        return _enumeator.MoveNextAsync();
    }

    public ValueTask DisposeAsync()
    {
        return new ValueTask(Task.CompletedTask);
    }

    public Task<string[]> KeysAsync()
    {
        return _cursor.KeysAsync();
    }

    public Task<IResultSummary> ConsumeAsync()
    {
        _isConsumed = true;
        return _cursor.ConsumeAsync();
    }


    public Task<IRecord> PeekAsync()
    {
        AssertNotConsumed();
        return _cursor.PeekAsync();
    }

    public Task<bool> FetchAsync()
    {
        AssertNotConsumed();
        return MoveNextAsync().AsTask();
    }

    public IRecord Current
    {
        get
        {
            AssertNotConsumed();
            return _cursor.Current;
        }
    }

    public bool IsOpen => !_isConsumed;

    public void Cancel()
    {
        _cursor.Cancel();
    }

    public IAsyncEnumerator<IRecord> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return this;
    }

    private void AssertNotConsumed()
    {
        if (_isConsumed)
        {
            throw ErrorExtensions.NewResultConsumedException();
        }
    }
}
