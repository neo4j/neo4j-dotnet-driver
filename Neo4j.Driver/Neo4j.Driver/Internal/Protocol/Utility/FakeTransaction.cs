// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
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
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal;

/// <summary>
/// Supports cursors on auto commit functions so we don't need to null check and check error.
/// this will always return false for the transaction has errored because the transaction doen't exist.
/// </summary>
internal sealed class FakeTransaction : IInternalAsyncTransaction
{
    internal static IInternalAsyncTransaction Instance = new FakeTransaction();

    private FakeTransaction()
    {
    }
    
    public void Dispose()
    {
    }

    public ValueTask DisposeAsync()
    {
        return new ValueTask(Task.CompletedTask);
    }

    public Task<IResultCursor> RunAsync(string query)
    {
        throw new InvalidOperationException("Something very wrong has happened and an illegal function has been called.");
    }

    public Task<IResultCursor> RunAsync(string query, object parameters)
    {
        throw new InvalidOperationException(
            "Something very wrong has happened and an illegal function has been called.");
    }

    public Task<IResultCursor> RunAsync(string query, IDictionary<string, object> parameters)
    {
        throw new InvalidOperationException(
            "Something very wrong has happened and an illegal function has been called.");
    }

    public Task<IResultCursor> RunAsync(Query query)
    {
        throw new InvalidOperationException(
            "Something very wrong has happened and an illegal function has been called.");
    }

    public TransactionConfig TransactionConfig => throw new InvalidOperationException(
        "Something very wrong has happened and an illegal function has been called.");
    public Task CommitAsync()
    {
        throw new InvalidOperationException(
            "Something very wrong has happened and an illegal function has been called.");
    }

    public Task RollbackAsync()
    {
        throw new InvalidOperationException(
            "Something very wrong has happened and an illegal function has been called.");
    }

    public bool IsErrored(out Exception ex)
    {
        ex = null;
        return false;
    }

    public bool IsOpen => throw new InvalidOperationException(
        "Something very wrong has happened and an illegal function has been called.");
}
