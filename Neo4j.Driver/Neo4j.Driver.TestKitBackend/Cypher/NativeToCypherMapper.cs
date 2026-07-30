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

namespace Neo4j.Driver.TestKitBackend.Cypher;

internal interface INativeToCypherMapper
{
    ICypherValue Map(object? value);
}

internal class NativeToCypherMapper : INativeToCypherMapper
{
    public ICypherValue Map(object? value)
    {
        return value switch
        {
            null => new CypherNull(),
            bool b => new CypherBool(b),
            long l => new CypherInt(l),
            double d => new CypherFloat(d),
            string s => new CypherString(s),
            List<object> list => new CypherList([..list.Select(Map)]),
            _ => throw new NotSupportedException($"No cypher mapping for native type {value.GetType().Name}")
        };
    }
}
