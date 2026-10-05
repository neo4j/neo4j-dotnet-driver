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

namespace Neo4j.Driver.Internal.QueryApi;

internal class QueryApiTransactionContextTracker : IQueryApiTransactionContextTracker, IDisposable
{
    private readonly ILoggingContextTracker _logContextTracker;

    private IDisposable? _loggingContext;

    public QueryApiTransactionContextTracker(ILoggingContextTracker logContextTracker)
    {
        _logContextTracker = logContextTracker;
    }

    public QueryApiTransactionContext? Context { get; private set; }
    public bool IsFailed { get; private set; }

    public void Set(QueryApiTransactionContext context)
    {
        Context = context;
        _loggingContext = _logContextTracker.Add("transaction", context);
    }

    public void MarkFailed() => IsFailed = true;

    public void Dispose()
    {
        _loggingContext?.Dispose();
        _loggingContext = null;
    }
}
