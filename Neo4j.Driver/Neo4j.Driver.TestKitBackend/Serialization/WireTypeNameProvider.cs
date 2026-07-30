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

using System.Runtime.CompilerServices;

namespace Neo4j.Driver.TestKitBackend.Serialization;

internal class WireTypeNameProvider : IWireTypeNameProvider
{
    // Wire names are needed before any instance exists (building the inbound name→type map),
    // but they are instance members so a type can override them. An uninitialized instance is
    // safe because name implementations must never touch instance state.
    public string GetInboundTypeName(Type type)
    {
        return ((IWireType)RuntimeHelpers.GetUninitializedObject(type)).InboundTypeName;
    }
}
