// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
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

namespace Neo4j.Driver.Internal.MessageHandling.Messages
{
    public class RouteMessageV43Tests
    {
        [Fact]
        public void ShouldHaveCorrectSerializer()
        {
            var rm = new RouteMessageV43(null, null, null);

            rm.Serializer.Should().BeOfType<RouteMessageSerializerV43>();
        }

        [Fact]
        public void ShouldHandleNullValues()
        {
            var rm = new RouteMessageV43(null, null, null);

            rm.Routing.Should().NotBeNull();
            rm.Bookmarks.Should().NotBeNull();
            rm.DatabaseParam.Should().BeNull();
            rm.ToString().Should().Be("ROUTE { } [] None");
        }

        [Fact]
        public void ShouldHandleSetValues()
        {
            var rc = new Dictionary<string, string>
            {
                ["a"] = "b"
            };

            var bm = new InternalBookmarks("bm:a");
            var db = "neo4j";

            var rm = new RouteMessageV43(rc, bm, db);

            rm.Routing.Should().BeEquivalentTo(rc);
            rm.Bookmarks.Values.Should().BeEquivalentTo(bm.Values);
            rm.DatabaseParam.Should().Be("neo4j");

            rm.ToString().Should().Be("ROUTE { 'a':'b' } { bookmarks, [bm:a] } 'neo4j'");
        }
    }
}
