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

using System.Linq;
using System.Net;
using System.Net.Http;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.DependencyInjection;
using Neo4j.Driver.Internal.QueryApi;
using Neo4j.Driver.Internal.QueryApi.Implementations;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

/// <summary>
/// The Query API uses the <c>neo4j-cluster-affinity</c> header to pin subsequent requests in a transaction to the
/// same cluster member, which is required for Aura instances. Spec:
/// https://neo4j.com/docs/query-api/current/#_cluster_affinity
/// </summary>
public class QueryApiClusterAffinityApplicatorTests
{
    private static QueryApiClusterAffinityApplicator WithContext(QueryApiTransactionContext? context)
    {
        var scoped = new Mock<IScoped<QueryApiTransactionContext>>();
        if (context is not null)
        {
            scoped.Setup(s => s.TryGetValue(out context)).Returns(true);
        }
        else
        {
            QueryApiTransactionContext? nullContext = null;
            scoped.Setup(s => s.TryGetValue(out nullContext)).Returns(false);
        }

        return new QueryApiClusterAffinityApplicator(scoped.Object);
    }

    public class Apply
    {
        [Fact]
        public void AddsClusterAffinityHeader_WhenContextCarriesAffinityValue()
        {
            var request = new HttpRequestMessage();

            WithContext(new QueryApiTransactionContext("tx-1", "shard-42")).Apply(request);

            request.Headers.TryGetValues("neo4j-cluster-affinity", out var values).Should().BeTrue();
            values!.First().Should().Be("shard-42");
        }

        [Fact]
        public void DoesNotAddHeader_WhenContextHasNoClusterAffinity()
        {
            var request = new HttpRequestMessage();

            WithContext(new QueryApiTransactionContext("tx-1", null)).Apply(request);

            request.Headers.Contains("neo4j-cluster-affinity").Should().BeFalse();
        }

        [Fact]
        public void DoesNotAddHeader_WhenNoTransactionContextIsAvailable()
        {
            var request = new HttpRequestMessage();

            WithContext(null).Apply(request);

            request.Headers.Contains("neo4j-cluster-affinity").Should().BeFalse();
        }
    }

    public class Extract
    {
        [Fact]
        public void ReturnsAffinityValue_WhenResponseCarriesHeader()
        {
            var response = new HttpResponseMessage(HttpStatusCode.Accepted);
            response.Headers.TryAddWithoutValidation("neo4j-cluster-affinity", "shard-42");

            var affinity = WithContext(null).Extract(response);

            affinity.Should().Be("shard-42");
        }

        [Fact]
        public void ReturnsNull_WhenResponseDoesNotCarryHeader()
        {
            var response = new HttpResponseMessage(HttpStatusCode.Accepted);

            var affinity = WithContext(null).Extract(response);

            affinity.Should().BeNull();
        }

        [Fact]
        public void JoinsMultipleHeaderValues_WithComma()
        {
            var response = new HttpResponseMessage(HttpStatusCode.Accepted);
            response.Headers.TryAddWithoutValidation("neo4j-cluster-affinity", "shard-1");
            response.Headers.TryAddWithoutValidation("neo4j-cluster-affinity", "shard-2");

            var affinity = WithContext(null).Extract(response);

            affinity.Should().Be("shard-1,shard-2");
        }
    }
}
