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
using System.Linq;

namespace Neo4j.Driver.Internal;

internal class ContextualLogger : ILogger
{
    private readonly ILoggingContextTracker _tracker;
    private ILogger _downstream;

    public ContextualLogger(
        ILoggingContextTracker tracker,
        ILogger downstream)
    {
        _tracker = tracker;
        _downstream = downstream;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        using var scope = _downstream.BeginScope(_tracker.Contexts.AsMicrosoftStateItems().ToList());
        _downstream.Log(logLevel, eventId, state, exception, formatter);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return _downstream.IsEnabled(logLevel);
    }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        return _downstream.BeginScope(state);
    }
}

