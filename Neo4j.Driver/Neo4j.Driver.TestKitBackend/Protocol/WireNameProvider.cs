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

internal class WireNameProvider : IRequestWireNameProvider, IResponseWireNameProvider
{
    public string GetRequestWireName(Type messageType) => StripSuffix(messageType.Name, "Request");

    public string GetResponseWireName(Type messageType) => StripSuffix(messageType.Name, "Response");

    private static string StripSuffix(string name, string suffix)
    {
        return name.EndsWith(suffix, StringComparison.Ordinal)
            ? name[..^suffix.Length]
            : name;
    }
}
