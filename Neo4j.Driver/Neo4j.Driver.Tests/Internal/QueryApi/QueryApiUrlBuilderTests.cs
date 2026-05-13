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

using System;
using FluentAssertions;
using Neo4j.Driver.Internal.QueryApi;
using Neo4j.Driver.Internal.QueryApi.Implementations;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

/// Spec: https://neo4j.com/docs/query-api/current/#_endpoints
public class QueryApiUrlBuilderTests
{
    private static QueryApiUrlBuilder BuilderFor(string uri)
    {
        var driverContext = TestDriverContext.With(uri: new Uri(uri));
        return new QueryApiUrlBuilder(driverContext);
    }

    [Fact]
    public void Build_ProducesCorrectAbsoluteUrl()
    {
        var url = BuilderFor("https://mydb.example.com:7474").Build("db/neo4j/query/v2");

        url.Should().Be(new Uri("https://mydb.example.com:7474/db/neo4j/query/v2"));
    }

    [Fact]
    public void Build_ToleratesTrailingSlashOnBaseUri()
    {
        var url = BuilderFor("https://localhost:7474/").Build("db/neo4j/query/v2");

        url.AbsoluteUri.Should().Be("https://localhost:7474/db/neo4j/query/v2");
    }

    [Fact]
    public void Build_ToleratesLeadingSlashOnPath()
    {
        var url = BuilderFor("https://localhost:7474").Build("/db/neo4j/query/v2");

        url.AbsoluteUri.Should().Be("https://localhost:7474/db/neo4j/query/v2");
    }

    [Fact]
    public void Build_PreservesSchemeHostAndPort()
    {
        var url = BuilderFor("https://aura.example.com:443").Build("db/system/query/v2");

        url.Scheme.Should().Be("https");
        url.Host.Should().Be("aura.example.com");
        url.Port.Should().Be(443);
    }
}
