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

using System;
using System.Collections.Generic;
using Neo4j.Driver.Internal.QueryApi.Abstractions;

namespace Neo4j.Driver.Internal.QueryApi.Implementations;

internal class LoggingContextTracker : ILoggingContextTracker
{
    private readonly List<ILoggingContext> _contexts = [];

    public IReadOnlyList<ILoggingContext> Contexts => _contexts;

    public IDisposable Add(string key, object value)
    {
        var ctx = new LoggingContext(key, value);
        _contexts.Add(ctx);
        return new ContextHandle(() => _contexts.Remove(ctx));
    }

    private sealed class ContextHandle(Action remove) : IDisposable
    {
        public void Dispose() => remove();
    }
}
