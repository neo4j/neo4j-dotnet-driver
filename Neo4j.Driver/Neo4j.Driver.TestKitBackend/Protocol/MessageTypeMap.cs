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

namespace Neo4j.Driver.TestKitBackend.Protocol;

internal class MessageTypeMap : IMessageTypeMap
{
    private readonly IReadOnlyDictionary<string, Type> _byName;

    public MessageTypeMap(IEnumerable<Type> messageTypes, IRequestWireNameProvider wireNameProvider)
    {
        _byName = messageTypes.ToDictionary(wireNameProvider.GetRequestWireName);
    }

    public Type GetTypeByName(string name) =>
        _byName.TryGetValue(name, out var type)
            ? type
            : throw new TestKitProtocolException($"Unrecognized message name \"{name}\".");
}
