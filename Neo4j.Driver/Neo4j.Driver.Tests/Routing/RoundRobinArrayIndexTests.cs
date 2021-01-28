// Copyright (c) "Neo4j"
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
using FluentAssertions;
using Neo4j.Driver.Internal.Routing;
using Xunit;

namespace Neo4j.Driver.Tests.Routing
{
    public class RoundRobinArrayIndexTests
    {
        [Fact]
        public void ShouldHandleZeroLength()
        {
            var roundRobinIndex = new RoundRobinArrayIndex();

            int index = roundRobinIndex.Next(0);

            index.Should().Be(-1);
        }

        [Fact]
        public void ShouldReturnIndexesInRoundRobinOrder()
        {
            RoundRobinArrayIndex roundRobinIndex = new RoundRobinArrayIndex();

            for (int i = 0; i < 10; i++)
            {
                int index = roundRobinIndex.Next(10);
                index.Should().Be(i);
            }

            for (int i = 0; i < 5; i++)
            {
                int index = roundRobinIndex.Next(5);
                index.Should().Be(i);
            }
        }

        [Fact]
        public void ShouldHandleOverflow()
        {
            int arrayLength = 10;
            RoundRobinArrayIndex roundRobinIndex = new RoundRobinArrayIndex(Int32.MaxValue - 2);

            roundRobinIndex.Next(arrayLength).Should().Be((Int32.MaxValue - 1) % arrayLength);
            roundRobinIndex.Next(arrayLength).Should().Be(Int32.MaxValue % arrayLength);
            roundRobinIndex.Next(arrayLength).Should().Be(0);
            roundRobinIndex.Next(arrayLength).Should().Be(1);
            roundRobinIndex.Next(arrayLength).Should().Be(2);
        }
    }
}
