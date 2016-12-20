using System;
using System.Collections.Generic;
using System.Diagnostics;
using FluentAssertions;
using Neo4j.Driver.Internal.Routing;
using Xunit;

namespace Neo4j.Driver.Tests.Routing
{
    public class RoundRobinRoutingTableTests
    {
        public class IsStatleMethod
        {
            private IEnumerable<Uri> CreateUriArray(int count)
            {
                var uris = new Uri[count];
                for (int i = 0; i < count; i++)
                {
                    uris[i] = new Uri($"http://neo4j:1{i}");
                }
                return uris;
            }

            [Theory]
            [InlineData(1, 2, 1, 5*60, false)] // 1 router, 2 reader, 1 writer
            [InlineData(0, 2, 1, 5*60, true)] // no router
            [InlineData(2, 2, 0, 5*60, true)] // no writer
            [InlineData(2, 2, 0, 5*60, true)] // no reader
            [InlineData(1, 2, 1, 0, true)] // expire immediately
            public void ShouldBeStateIfOnlyHaveOneRouter(int routerCount, int readerCount, int writerCount, long expireAfterSeconds, bool isStale)
            {
                var table = new RoundRobinRoutingTable(
                    CreateUriArray(routerCount),
                    CreateUriArray(readerCount),
                    CreateUriArray(writerCount),
                    new Stopwatch(),
                    expireAfterSeconds);
                table.IsStale().Should().Be(isStale);
            }
        } 
    }
}
