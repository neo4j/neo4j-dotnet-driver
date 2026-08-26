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

namespace Neo4j.Driver.TestKitBackend.Serialization;

internal abstract class WireTypeMap : IWireTypeResolver
{
    private readonly IReadOnlyDictionary<string, Type> _byName;

    protected WireTypeMap(IEnumerable<Type> types, IWireTypeNameProvider wireTypeNameProvider)
    {
        var byName = new Dictionary<string, Type>();
        foreach (var type in types)
        {
            var name = wireTypeNameProvider.GetInboundTypeName(type);
            if (byName.TryGetValue(name, out var existing))
            {
                throw new TestKitProtocolException(
                    $"Wire name '{name}' is claimed by both {existing.FullName} and {type.FullName}");
            }

            byName[name] = type;
        }

        _byName = byName;
    }

    public Type GetTypeByName(string name)
    {
        return _byName.TryGetValue(name, out var type)
            ? type
            : throw new TestKitProtocolException($"Unrecognized wire type name \"{name}\".");
    }
}
