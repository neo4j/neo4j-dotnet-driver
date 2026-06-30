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

namespace Neo4j.Driver.Tests.TestBackend.Protocol.Skip;

internal abstract record TestDisposition
{
    public sealed record RunAll : TestDisposition;

    public sealed record RunSubtests : TestDisposition;

    public sealed record SkipAll(string Reason) : TestDisposition;
}

internal static class TestDispositions
{
    public static TestDisposition RunAll { get; } = new TestDisposition.RunAll();

    public static TestDisposition RunSubtests { get; } = new TestDisposition.RunSubtests();

    public static TestDisposition SkipAll(string reason) => new TestDisposition.SkipAll(reason);
}
