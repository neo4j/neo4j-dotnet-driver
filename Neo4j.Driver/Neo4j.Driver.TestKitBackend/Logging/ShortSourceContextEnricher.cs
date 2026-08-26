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

using Serilog.Core;
using Serilog.Events;

namespace Neo4j.Driver.TestKitBackend.Logging;

internal class ShortSourceContextEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (!logEvent.Properties.TryGetValue("SourceContext", out var value) ||
            value is not ScalarValue { Value: string sourceContext })
        {
            return;
        }

        var shortName = sourceContext[(sourceContext.LastIndexOf('.') + 1)..];
        logEvent.AddPropertyIfAbsent(new LogEventProperty("SourceContextShort", new ScalarValue($" [{shortName}]")));
    }
}
