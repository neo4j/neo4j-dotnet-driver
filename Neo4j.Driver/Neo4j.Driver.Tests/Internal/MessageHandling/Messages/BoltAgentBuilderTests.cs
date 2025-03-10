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

using FluentAssertions;
using Neo4j.Driver.Internal.Messaging.Utils;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.MessageHandling.Messages;

public class BoltAgentBuilderTests
{
    [Fact]
    public void ShouldReturnBoltAgent()
    {
        var agent = BoltAgentBuilder.Agent;
        agent.Should()
            .HaveCount(3)
            .And.ContainKey("platform")
            .And.ContainKey("language_details")
            .And.ContainKey("product")
            .WhoseValue
            .Should()
            .MatchRegex(@"^neo4j-dotnet/\d\.\d+\.\d+$");
    }
}
