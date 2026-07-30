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

using System.Text.Json.Serialization;

namespace Neo4j.Driver.TestKitBackend.Cypher;

internal record CypherNull : ICypherValue
{
    public object? Value { get; init; }
}

internal record CypherBool(bool Value) : ICypherValue;

internal record CypherInt(long Value) : ICypherValue;

internal record CypherString(string Value) : ICypherValue;

internal record CypherFloat([property: JsonConverter(typeof(CypherFloatValueConverter))] double Value) : ICypherValue;

internal record CypherUuid(Guid Value) : ICypherValue;
