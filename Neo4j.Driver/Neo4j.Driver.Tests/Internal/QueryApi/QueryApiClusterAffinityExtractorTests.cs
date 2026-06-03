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

#nullable enable

using System.Net;
using System.Net.Http;
using FluentAssertions;
using Neo4j.Driver.Internal.QueryApi.Implementations;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

public class QueryApiClusterAffinityExtractorTests
{
    private readonly QueryApiClusterAffinityExtractor _subject = new();

    [Fact]
    public void ReturnsAffinityValue_WhenResponseCarriesHeader()
    {
        var response = new HttpResponseMessage(HttpStatusCode.Accepted);
        response.Headers.TryAddWithoutValidation("neo4j-cluster-affinity", "shard-42");

        _subject.Extract(response).Should().Be("shard-42");
    }

    [Fact]
    public void ReturnsNull_WhenResponseDoesNotCarryHeader()
    {
        var response = new HttpResponseMessage(HttpStatusCode.Accepted);

        _subject.Extract(response).Should().BeNull();
    }

    [Fact]
    public void JoinsMultipleHeaderValues_WithComma()
    {
        var response = new HttpResponseMessage(HttpStatusCode.Accepted);
        response.Headers.TryAddWithoutValidation("neo4j-cluster-affinity", "shard-1");
        response.Headers.TryAddWithoutValidation("neo4j-cluster-affinity", "shard-2");

        _subject.Extract(response).Should().Be("shard-1,shard-2");
    }
}
