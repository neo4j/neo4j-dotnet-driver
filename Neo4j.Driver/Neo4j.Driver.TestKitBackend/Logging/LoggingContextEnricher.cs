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

using System.Text.Json;
using Serilog.Core;
using Serilog.Events;

namespace Neo4j.Driver.TestKitBackend.Logging;

internal class LoggingContextEnricher : ILogEventEnricher
{
    private readonly ILoggingContextAccessor _accessor;

    public LoggingContextEnricher(ILoggingContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var context = _accessor.GetCurrent();
        if (context is null || context.Current.Count == 0)
        {
            return;
        }

        // Leading space instead of a template separator, so absent context leaves no gap; the
        // template renders this after the message, at the end of the line.
        var json = JsonSerializer.Serialize(context.Current);
        logEvent.AddPropertyIfAbsent(new LogEventProperty("LoggingContext", new ScalarValue(" " + json)));
    }
}
