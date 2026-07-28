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

using System.Collections.Immutable;

namespace Neo4j.Driver.TestKitBackend.Logging;

// One per connection scope: handlers mutate it, the connection handler publishes it to the
// accessor so the process-wide enricher can find it.
[RegistrationLifetime(RegistrationLifetime.PerLifetimeScope)]
internal class LoggingContext : ILoggingContext
{
    // Immutable snapshot swapped on write, so the enricher always reads a consistent dictionary.
    private ImmutableDictionary<string, object?> _entries = ImmutableDictionary<string, object?>.Empty;

    public void Set(string key, object? value)
    {
        _entries = _entries.SetItem(key, value);
    }

    public void Remove(string key)
    {
        _entries = _entries.Remove(key);
    }

    public IReadOnlyDictionary<string, object?> Current => _entries;
}
