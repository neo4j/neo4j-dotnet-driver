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

using System.Collections.Generic;
using FluentAssertions;
using Neo4j.Driver.Internal.IO.MessageSerializers;
using Neo4j.Driver.Internal.Messaging;
using Xunit;

namespace Neo4j.Driver.Internal.MessageHandling.Messages;

public class RouteMessageTests
{
    [Fact]
    public void ShouldHaveCorrectSerializer()
    {
        var rm = new RouteMessage(null, null, null, null);

        rm.Serializer.Should().BeOfType<RouteMessageSerializer>();
    }

    [Fact]
    public void ShouldHandleNullValues()
    {
        var rm = new RouteMessage(null, null, null, null);

        rm.Routing.Should().NotBeNull();
        rm.Bookmarks.Should().NotBeNull();
        rm.DatabaseContext.Should().NotBeNull();

        rm.ToString().Should().Be("ROUTE { } [] { }");
    }

    [Fact]
    public void ShouldHandleSetValues()
    {
        var routingContext = new Dictionary<string, string>
        {
            ["a"] = "b"
        };

        var bookmarks = new InternalBookmarks("bm:a");
        var databaseName = "neo4j";
        var impersonatedUser = "Douglas Fir";

        var rm = new RouteMessage(routingContext, bookmarks, databaseName, impersonatedUser);

        rm.Routing.Should().BeEquivalentTo(routingContext);
        rm.Bookmarks.Values.Should().BeEquivalentTo(bookmarks.Values);

        rm.ToString()
            .Should()
            .Be("ROUTE { 'a':'b' } { bookmarks, [bm:a] } { 'db':'neo4j' 'imp_user':'Douglas Fir' }");
    }
}
