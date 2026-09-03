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

using Neo4j.Driver.TestKitBackend.Cypher;
using Neo4j.Driver.TestKitBackend.Dispatch;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal record GqlErrorResponse : IProtocolMessage
{
    public required string Msg { get; init; }
    public string? GqlStatus { get; init; }
    public string? StatusDescription { get; init; }
    public string? Classification { get; init; }
    public string? RawClassification { get; init; }
    public IReadOnlyDictionary<string, ICypherValue>? DiagnosticRecord { get; init; }
    public IProtocolMessage? Cause { get; init; }
}
