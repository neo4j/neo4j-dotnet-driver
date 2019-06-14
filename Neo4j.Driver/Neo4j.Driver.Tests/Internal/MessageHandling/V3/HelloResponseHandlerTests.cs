// Copyright (c) 2002-2019 "Neo4j,"
// Neo4j Sweden AB [http://neo4j.com]
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
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.MessageHandling.Metadata;
using Neo4j.Driver.Internal.Util;
using Xunit;

namespace Neo4j.Driver.Internal.MessageHandling.V3
{
    public class HelloResponseHandlerTests
    {
        [Fact]
        public void ShouldThrowIfConnectionIsNull()
        {
            var exc = Record.Exception(() => new HelloResponseHandler(null));

            exc.Should().BeOfType<ArgumentNullException>().Which
                .ParamName.Should().Be("connection");
        }

        [Fact]
        public void ShouldUpdateVersionAndConnectionId()
        {
            var tracker = new Mock<IConnection>();
            var handler = new HelloResponseHandler(tracker.Object);

            handler.OnSuccess(new[]
                {ServerVersionCollectorTests.TestMetadata, ConnectionIdCollectorTests.TestMetadata}.ToDictionary());

            tracker.Verify(x => x.UpdateVersion(ServerVersionCollectorTests.TestMetadataCollected), Times.Once);
            tracker.Verify(x => x.UpdateId(ConnectionIdCollectorTests.TestMetadataCollected), Times.Once);
        }
    }
}