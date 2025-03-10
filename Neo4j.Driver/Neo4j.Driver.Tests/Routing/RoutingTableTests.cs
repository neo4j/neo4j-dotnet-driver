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
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Neo4j.Driver.Internal.Routing;
using Xunit;

namespace Neo4j.Driver.Tests.Routing;

public static class RoutingTableTests
{
    private static IEnumerable<Uri> CreateUriArray(int count)
    {
        var uris = new Uri[count];
        for (var i = 0; i < count; i++)
        {
            uris[i] = new Uri($"http://neo4j:1{i}");
        }

        return uris;
    }

    public class Constructor
    {
        [Fact]
        public void ShouldEnsureInitialRouter()
        {
            var initUri = new Uri("bolt://123:456");
            var routers = new HashSet<Uri> { initUri };
            var table = new RoutingTable(null, routers);

            var uri = table.Routers.Single();
            uri.Should().Be(initUri);

            table.All().Single().Should().Be(initUri);
        }

        [Fact]
        public void ShouldNormalizeNullDatabaseToEmptyString()
        {
            var routingTable = new RoutingTable(null, new[] { new Uri("bolt://my-router") }, 1);

            routingTable.Database.Should().BeEmpty();
        }
    }

    public class IsStaleMethod
    {
        [Theory]
        [InlineData(1, 2, 1, 5 * 60, false)] // 1 router, 2 reader, 1 writer
        [InlineData(0, 2, 1, 5 * 60, true)] // no router
        [InlineData(2, 2, 0, 5 * 60, false)] // no writer
        [InlineData(2, 0, 2, 5 * 60, true)] // no reader
        [InlineData(1, 2, 1, -1, true)] // expire immediately
        public void ShouldBeStaleInReadModeIfOnlyHaveOneRouter(
            int routerCount,
            int readerCount,
            int writerCount,
            long expireAfterSeconds,
            bool isStale)
        {
            var table = new RoutingTable(
                null,
                CreateUriArray(routerCount),
                CreateUriArray(readerCount),
                CreateUriArray(writerCount),
                expireAfterSeconds);

            table.IsStale(AccessMode.Read).Should().Be(isStale);
        }

        [Theory]
        [InlineData(1, 2, 1, 5 * 60, false)] // 1 router, 2 reader, 1 writer
        [InlineData(0, 2, 1, 5 * 60, true)] // no router
        [InlineData(2, 2, 0, 5 * 60, true)] // no writer
        [InlineData(2, 0, 2, 5 * 60, false)] // no reader
        [InlineData(1, 2, 1, -1, true)] // expire immediately
        public void ShouldBeStaleInWriteModeIfOnlyHaveOneRouter(
            int routerCount,
            int readerCount,
            int writerCount,
            long expireAfterSeconds,
            bool isStale)
        {
            var table = new RoutingTable(
                null,
                CreateUriArray(routerCount),
                CreateUriArray(readerCount),
                CreateUriArray(writerCount),
                expireAfterSeconds);

            table.IsStale(AccessMode.Write).Should().Be(isStale);
        }
    }

    public class PrependRoutersMethod
    {
        [Fact]
        public void ShouldInjectInFront()
        {
            // Given
            var table = new RoutingTable(
                null,
                CreateUriArray(3),
                CreateUriArray(0),
                CreateUriArray(0),
                5 * 60);

            var router = table.Routers[0];
            var head = new Uri("http://neo4j:10");
            router.Should().Be(head);

            // When
            var first = new Uri("me://12");
            var second = new Uri("me://22");
            table.PrependRouters(new List<Uri> { first, second });

            // Then
            router = table.Routers[0];
            router.Should().Be(first);
            router = table.Routers[1];
            router.Should().Be(second);
            router = table.Routers[2];
            router.Should().Be(head);
        }
    }

    public class IsReadingInAbsenceOfWriterMethod
    {
        [Fact]
        public void ShouldReturnTrue()
        {
            var routingTable = new RoutingTable(
                null,
                new[] { new Uri("neo4j://my-router") },
                new[] { new Uri("neo4j://my-reader") },
                Enumerable.Empty<Uri>(),
                10);

            routingTable.IsReadingInAbsenceOfWriter(AccessMode.Read).Should().BeTrue();
        }

        [Fact]
        public void ShouldReturnFalseWhenExpired()
        {
            var routingTable = new RoutingTable(
                null,
                new[] { new Uri("neo4j://my-router") },
                new[] { new Uri("neo4j://my-reader") },
                Enumerable.Empty<Uri>(),
                -1);

            routingTable.IsReadingInAbsenceOfWriter(AccessMode.Read).Should().BeFalse();
        }

        [Fact]
        public void ShouldReturnFalseWhenAccessModeIsWrite()
        {
            var routingTable = new RoutingTable(
                null,
                new[] { new Uri("neo4j://my-router") },
                new[] { new Uri("neo4j://my-reader") },
                Enumerable.Empty<Uri>(),
                10);

            routingTable.IsReadingInAbsenceOfWriter(AccessMode.Write).Should().BeFalse();
        }

        [Fact]
        public void ShouldReturnFalseWhenThereIsAWriter()
        {
            var routingTable = new RoutingTable(
                null,
                new[] { new Uri("neo4j://my-router") },
                new[] { new Uri("neo4j://my-reader") },
                new[] { new Uri("neo4j://my-writer") },
                10);

            routingTable.IsReadingInAbsenceOfWriter(AccessMode.Read).Should().BeFalse();
        }

        [Fact]
        public void ShouldReturnFalseWhenNoReaders()
        {
            var routingTable = new RoutingTable(
                null,
                new[] { new Uri("neo4j://my-router") },
                Enumerable.Empty<Uri>(),
                Enumerable.Empty<Uri>(),
                10);

            routingTable.IsReadingInAbsenceOfWriter(AccessMode.Read).Should().BeFalse();
        }

        [Fact]
        public void ShouldReturnFalseWhenNoReadersButWriters()
        {
            var routingTable = new RoutingTable(
                null,
                new[] { new Uri("neo4j://my-router") },
                Enumerable.Empty<Uri>(),
                new[] { new Uri("neo4j://my-writer") },
                10);

            routingTable.IsReadingInAbsenceOfWriter(AccessMode.Read).Should().BeFalse();
        }

        [Fact]
        public void ShouldReturnFalseWhenNoRouters()
        {
            var routingTable = new RoutingTable(
                null,
                Enumerable.Empty<Uri>(),
                new[] { new Uri("neo4j://my-reader") },
                new[] { new Uri("neo4j://my-writer") },
                10);

            routingTable.IsReadingInAbsenceOfWriter(AccessMode.Read).Should().BeFalse();
        }
    }

    public class ToStringMethod
    {
        [Fact]
        public void ShouldGenerateCorrectString()
        {
            var routingTable = new RoutingTable(
                "foo",
                new[] { new Uri("bolt://my-router-1"), new Uri("neo4j://my-router-2") },
                new[] { new Uri("bolt://my-reader-1"), new Uri("neo4j://my-reader-2") },
                new[] { new Uri("bolt://my-writer-1"), new Uri("neo4j://my-writer-2") },
                10);

            routingTable.ToString()
                .Should()
                .Be(
                    "RoutingTable{" +
                    "database=foo, " +
                    "routers=[bolt://my-router-1/, neo4j://my-router-2/], " +
                    "writers=[bolt://my-writer-1/, neo4j://my-writer-2/], " +
                    "readers=[bolt://my-reader-1/, neo4j://my-reader-2/], " +
                    "expiresAfter=10s" +
                    "}");
        }
    }

    public class RemoveMethod
    {
        [Fact]
        public void ShouldRemoveFromAll()
        {
            var routingTable = new RoutingTable(
                "foo",
                new[]
                {
                    new Uri("bolt://my-server-1"), new Uri("neo4j://my-server-2"), new Uri("http://my-server-3")
                },
                new[]
                {
                    new Uri("bolt://my-server-1"), new Uri("neo4j://my-server-2"), new Uri("http://my-server-3")
                },
                new[]
                {
                    new Uri("bolt://my-server-1"), new Uri("neo4j://my-server-2"), new Uri("http://my-server-3")
                },
                10);

            routingTable.Remove(new Uri("neo4j://my-server-2"));

            routingTable.Database.Should().Be("foo");
            routingTable.Routers.Should()
                .BeEquivalentTo([new Uri("bolt://my-server-1"), new Uri("http://my-server-3")]);

            routingTable.Writers.Should()
                .BeEquivalentTo([new Uri("bolt://my-server-1"), new Uri("http://my-server-3")]);

            routingTable.Readers.Should()
                .BeEquivalentTo([new Uri("bolt://my-server-1"), new Uri("http://my-server-3")]);

            routingTable.ExpireAfterSeconds.Should().Be(10);
        }

        [Fact]
        public void ShouldNotRemoveIfNotPresent()
        {
            var routingTable = new RoutingTable(
                "foo",
                new[] { new Uri("bolt://my-server-1"), new Uri("neo4j://my-server-2") },
                new[] { new Uri("bolt://my-server-1"), new Uri("neo4j://my-server-2") },
                new[] { new Uri("bolt://my-server-1"), new Uri("neo4j://my-server-2") },
                10);

            routingTable.Remove(new Uri("neo4j://my-server-3"));

            routingTable.Database.Should().Be("foo");
            routingTable.Routers.Should()
                .BeEquivalentTo([new Uri("bolt://my-server-1"), new Uri("neo4j://my-server-2")]);

            routingTable.Writers.Should()
                .BeEquivalentTo([new Uri("bolt://my-server-1"), new Uri("neo4j://my-server-2")]);

            routingTable.Readers.Should()
                .BeEquivalentTo([new Uri("bolt://my-server-1"), new Uri("neo4j://my-server-2")]);

            routingTable.ExpireAfterSeconds.Should().Be(10);
        }
    }

    public class RemoveWriterMethod
    {
        [Fact]
        public void ShouldRemoveFromWritersOnly()
        {
            var routingTable = new RoutingTable(
                "foo",
                new[]
                {
                    new Uri("bolt://my-server-1"), new Uri("neo4j://my-server-2"), new Uri("http://my-server-3")
                },
                new[]
                {
                    new Uri("bolt://my-server-1"), new Uri("neo4j://my-server-2"), new Uri("http://my-server-3")
                },
                new[]
                {
                    new Uri("bolt://my-server-1"), new Uri("neo4j://my-server-2"), new Uri("http://my-server-3")
                },
                10);

            routingTable.RemoveWriter(new Uri("neo4j://my-server-2"));

            routingTable.Database.Should().Be("foo");
            routingTable.Routers.Should()
                .BeEquivalentTo(
                [
                    new Uri("bolt://my-server-1"),
                    new Uri("neo4j://my-server-2"),
                    new Uri("http://my-server-3")
                ]);

            routingTable.Writers.Should()
                .BeEquivalentTo([new Uri("bolt://my-server-1"), new Uri("http://my-server-3")]);

            routingTable.Readers.Should()
                .BeEquivalentTo(
                [
                    new Uri("bolt://my-server-1"),
                    new Uri("neo4j://my-server-2"),
                    new Uri("http://my-server-3")
                ]);

            routingTable.ExpireAfterSeconds.Should().Be(10);
        }

        [Fact]
        public void ShouldNotRemoveIfNotPresent()
        {
            var routingTable = new RoutingTable(
                "foo",
                new[] { new Uri("bolt://my-server-1"), new Uri("neo4j://my-server-2") },
                new[] { new Uri("bolt://my-server-1"), new Uri("neo4j://my-server-2") },
                new[] { new Uri("bolt://my-server-1"), new Uri("neo4j://my-server-2") },
                10);

            routingTable.RemoveWriter(new Uri("neo4j://my-server-3"));

            routingTable.Database.Should().Be("foo");
            routingTable.Routers.Should()
                .BeEquivalentTo([new Uri("bolt://my-server-1"), new Uri("neo4j://my-server-2")]);

            routingTable.Writers.Should()
                .BeEquivalentTo([new Uri("bolt://my-server-1"), new Uri("neo4j://my-server-2")]);

            routingTable.Readers.Should()
                .BeEquivalentTo([new Uri("bolt://my-server-1"), new Uri("neo4j://my-server-2")]);

            routingTable.ExpireAfterSeconds.Should().Be(10);
        }
    }

    public class AllMethod
    {
        [Fact]
        public void ShouldUnionAll()
        {
            var routingTable = new RoutingTable(
                "foo",
                new[]
                {
                    new Uri("bolt://my-server-1"), new Uri("neo4j://my-server-2"), new Uri("http://my-server-3")
                },
                new[] { new Uri("bolt://my-server-1"), new Uri("neo4j://my-server-2") },
                new[] { new Uri("bolt://my-server-1"), new Uri("neo4j://my-server-2") },
                10);

            routingTable.All()
                .Should()
                .BeEquivalentTo(
                [
                    new Uri("bolt://my-server-1"),
                    new Uri("neo4j://my-server-2"),
                    new Uri("http://my-server-3")
                ]);
        }

        [Fact]
        public void ShouldReturnEmptyWhenEmpty()
        {
            var routingTable = new RoutingTable(
                "foo",
                Enumerable.Empty<Uri>(),
                Enumerable.Empty<Uri>(),
                Enumerable.Empty<Uri>(),
                10);

            routingTable.All().Should().BeEmpty();
        }
    }

    public class IsExpiredForMethod
    {
        /*Theory]
        [InlineData(2, 1, 3000, true)]
        [InlineData(2, 0, 2000, true)]
        [InlineData(2, 2, 3000, false)]
        public async Task ShouldReturnExpectedValue(int expiresAfterSecs, int expiredForCheckSecs, int delayMs, bool expected)
        {
            var timer = new Mock<ITimer>();
            timer.Setup(x => x.ElapsedMilliseconds).Returns(0);

            var routingTable = new RoutingTable(null, new[] {new Uri("neo4j://my-router")},
                new[] {new Uri("neo4j://my-reader")}, Enumerable.Empty<Uri>(), expiresAfterSecs);


            await System.Threading.Tasks.Task.Delay(delayMs); // this needs fixing. Also remove the timer.Object constructor from the routingTable class.
            routingTable.IsExpiredFor(TimeSpan.FromSeconds(expiredForCheckSecs)).Should().Be(expected);
        }
        */
    }
}
