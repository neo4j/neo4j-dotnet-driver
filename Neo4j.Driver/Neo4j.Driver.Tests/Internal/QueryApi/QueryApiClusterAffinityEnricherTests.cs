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
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.Internal.QueryApi;
using Neo4j.Driver.Internal.QueryApi.Implementations;
using Neo4j.Driver.Internal.QueryApi.Types;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

public class QueryApiClusterAffinityEnricherTests
{
    public class Apply
    {
        [Fact]
        public async Task AddsClusterAffinityHeader_WhenContextCarriesAffinityValue()
        {
            var request = new HttpRequestMessage();
            var subject = new QueryApiClusterAffinityEnricher(new QueryApiTransactionContext("tx-1", "shard-42"));

            await subject.Enrich(request, TestContext.Current.CancellationToken);

            request.Headers.TryGetValues("neo4j-cluster-affinity", out var values).Should().BeTrue();
            values!.First().Should().Be("shard-42");
        }

        [Fact]
        public async Task DoesNotAddHeader_WhenContextHasNoClusterAffinity()
        {
            var request = new HttpRequestMessage();
            var subject = new QueryApiClusterAffinityEnricher(new QueryApiTransactionContext("tx-1", null));

            await subject.Enrich(request, TestContext.Current.CancellationToken);

            request.Headers.Contains("neo4j-cluster-affinity").Should().BeFalse();
        }
    }
}
