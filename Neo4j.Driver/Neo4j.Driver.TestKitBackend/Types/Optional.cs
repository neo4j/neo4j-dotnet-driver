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

namespace Neo4j.Driver.TestKitBackend.Types;

// A three-state wire value for the handful of fields where absent, present-null and present-value
// are all distinct (e.g. timeout, trustedCertificates) — the case Nullable<T> can't express
// because T? can't be nested. It's a struct so its default is "absent": a message record property
// needs no initializer, and an omitted key simply leaves it absent. Everywhere else, plain T? is
// enough (absent collapses to null) and Optional is not used.
//
// Read with the one-shot: `if (opt.IsSpecified(out var value)) ...; else /* absent */`.
internal readonly struct Optional<T>
{
    private readonly bool _isSpecified;
    private readonly T _value;

    private Optional(T value)
    {
        _isSpecified = true;
        _value = value;
    }

    public static Optional<T> Absent => default;

    public static Optional<T> Specified(T value)
    {
        return new Optional<T>(value);
    }

    public bool IsSpecified(out T value)
    {
        value = _value;
        return _isSpecified;
    }
}
