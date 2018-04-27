// Copyright (c) 2002-2018 Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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
using Neo4j.Driver.Internal.Result;
using Xunit;

namespace Neo4j.Driver.Tests
{
    public class SummaryBuilderTests
    {
        [Theory]
        [InlineData("bolt://localhost:7687", "1.2.3", "ServerInfo{Address=localhost:7687, Version=1.2.3}")]
        [InlineData("bolt://127.0.0.1:7687", "1.2.3", "ServerInfo{Address=127.0.0.1:7687, Version=1.2.3}")]
        // If no port provided, it will be port=-1. This should never happen as we always default to 7687 if no port provided.
        [InlineData("bolt://localhost", "1.2.3", "ServerInfo{Address=localhost:-1, Version=1.2.3}")]
        [InlineData("https://neo4j.com:9999", "1.2.3", "ServerInfo{Address=neo4j.com:9999, Version=1.2.3}")]
        public void CreateServerInfoCorrectly(string uriStr, string version, string expected)
        {
            var uri = new Uri(uriStr);
            var serverInfo = new ServerInfo(uri);
            serverInfo.Version = version;

            serverInfo.ToString().Should().Be(expected);
        }
    }
}
