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

using System.Reflection;

namespace Neo4j.Driver.Tests.TestBackend.Protocol;

internal class MessageRegistry : IMessageRegistry
{
    private readonly IReadOnlyDictionary<string, Type> _byName;

    public MessageRegistry(IEnumerable<Type> messageTypes)
    {
        _byName = messageTypes.ToDictionary(t => t.Name);
    }

    public static MessageRegistry FromAssembly(Assembly assembly)
    {
        var messageTypes = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IProtocolMessage).IsAssignableFrom(t));

        return new MessageRegistry(messageTypes);
    }

    public Type Resolve(string name)
    {
        return _byName.TryGetValue(name, out var type)
            ? type
            : throw new TestKitProtocolException($"Unrecognized message name \"{name}\".");
    }
}
