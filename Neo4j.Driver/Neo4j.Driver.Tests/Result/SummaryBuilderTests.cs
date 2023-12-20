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

using System;
using FluentAssertions;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Internal.Result;
using Xunit;

namespace Neo4j.Driver.Tests;

public class SummaryBuilderTests
{
    [Theory]
    [InlineData(
        "bolt://localhost:7687",
        "1.2.3",
        "ServerInfo{Address=localhost:7687, Agent=1.2.3, ProtocolVersion=1.2}")]
    [InlineData(
        "bolt://127.0.0.1:7687",
        "1.2.3",
        "ServerInfo{Address=127.0.0.1:7687, Agent=1.2.3, ProtocolVersion=1.2}")]
    // If no port provided, it will be port=-1. This should never happen as we always default to 7687 if no port provided.
    [InlineData("bolt://localhost", "1.2.3", "ServerInfo{Address=localhost:-1, Agent=1.2.3, ProtocolVersion=1.2}")]
    [InlineData(
        "https://neo4j.com:9999",
        "1.2.3",
        "ServerInfo{Address=neo4j.com:9999, Agent=1.2.3, ProtocolVersion=1.2}")]
    public void CreateServerInfoCorrectly(string uriStr, string version, string expected)
    {
        var uri = new Uri(uriStr);
        var serverInfo = new ServerInfo(uri);
        serverInfo.Update(new BoltProtocolVersion(1, 2), version);

        serverInfo.ToString().Should().Be(expected);
    }

    [Fact]
    public void ShouldReturnEmptyDatabaseInfoIfNotSet()
    {
        var builder = new SummaryBuilder(new Query("RETURN 1"), new ServerInfo(new Uri("bolt://localhost")));
        var summary = builder.Build();

        summary.Database.Should().NotBeNull();
        summary.Database.Name.Should().BeNull();
    }
}