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
using System.Net.Http;
using FluentAssertions;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.QueryApi;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

public class QueryApiRequestHeaderWriterTests
{
    private readonly QueryApiRequestHeaderWriter _subject = new();

    [Fact]
    public void ApplyMediaType_V1_0_SetsAcceptAndContentType()
    {
        var request = new HttpRequestMessage { Content = new StringContent("{}") };

        _subject.ApplyMediaType(request, QueryApiMediaVersion.V1_0);

        request.Headers.Accept.Should().Contain(h => h.MediaType == "application/vnd.neo4j.query.v1.0");
        request.Content!.Headers.ContentType!.MediaType.Should().Be("application/vnd.neo4j.query.v1.0");
    }

    [Fact]
    public void ApplyMediaType_V1_1_SetsAcceptAndContentType()
    {
        var request = new HttpRequestMessage { Content = new StringContent("{}") };

        _subject.ApplyMediaType(request, QueryApiMediaVersion.V1_1);

        request.Headers.Accept.Should().Contain(h => h.MediaType == "application/vnd.neo4j.query.v1.1");
        request.Content!.Headers.ContentType!.MediaType.Should().Be("application/vnd.neo4j.query.v1.1");
    }

    [Theory]
    [InlineData(nameof(QueryApiMediaVersion.V1_0))]
    [InlineData(nameof(QueryApiMediaVersion.V1_1))]   
    public void ApplyMediaType_AlwaysAddsJsonFallbackToAccept(string versionString)
    {
        var version = Enum.Parse<QueryApiMediaVersion>(versionString);
        var request = new HttpRequestMessage { Content = new StringContent("{}") };

        _subject.ApplyMediaType(request, version);

        request.Headers.Accept.Should().Contain(h => h.MediaType == "application/json" && h.Quality == 0.9);
    }

    [Fact]
    public void ApplyMediaType_ContentType_HasNoCharset()
    {
        // The server rejects requests carrying a charset parameter on this media type.
        var request = new HttpRequestMessage { Content = new StringContent("{}") };

        _subject.ApplyMediaType(request, QueryApiMediaVersion.V1_0);

        request.Content!.Headers.ContentType!.CharSet.Should().BeNull();
    }

    [Fact]
    public void ApplyMediaType_WithoutContent_SetsAcceptOnly()
    {
        var request = new HttpRequestMessage();

        _subject.ApplyMediaType(request, QueryApiMediaVersion.V1_0);

        request.Headers.Accept.Should().Contain(h => h.MediaType == "application/vnd.neo4j.query.v1.0");
        request.Content.Should().BeNull();
    }
}
