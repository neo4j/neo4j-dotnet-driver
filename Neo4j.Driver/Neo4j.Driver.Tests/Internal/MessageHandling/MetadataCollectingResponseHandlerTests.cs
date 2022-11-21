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

using System;
using System.Collections.Generic;
using FluentAssertions;
using Moq;
using Xunit;

namespace Neo4j.Driver.Internal.MessageHandling;

public class MetadataCollectingResponseHandlerTests
{
    [Fact]
    public void ShouldCallCollectOnCollector()
    {
        var collector = new Mock<IMetadataCollector<long>>();

        var handler = new TestHandler();
        handler.AddCollector<IMetadataCollector<long>, long>(collector.Object);

        var metadata = new Dictionary<string, object> { { "x", 1 }, { "y", false } };

        handler.OnSuccess(metadata);

        collector.Verify(x => x.Collect(metadata), Times.Once);
    }

    [Fact]
    public void ShouldNotAddSameCollectorTypeTwice()
    {
        var (collector1, collector2) = (new Mock<IMetadataCollector<long>>(), new Mock<IMetadataCollector<long>>());

        var handler = new TestHandler();
        handler.AddCollector<IMetadataCollector<long>, long>(collector1.Object);

        var exc = Record.Exception(() => handler.AddCollector<IMetadataCollector<long>, long>(collector2.Object));

        exc.Should().BeOfType<InvalidOperationException>();
    }

    private class TestHandler : MetadataCollectingResponseHandler
    {
        public void AddCollector<TCollector, TMetadata>(TCollector collector)
            where TCollector : class, IMetadataCollector<TMetadata>
        {
            AddMetadata<TCollector, TMetadata>(collector);
        }
    }
}
