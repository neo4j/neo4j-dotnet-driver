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
using Xunit;
using static Neo4j.Driver.Tests.AsyncSessionTests;

namespace Neo4j.Driver.Tests;

public class BookmarkTests
{
    public class ConstructorMethod
    {
        public static IEnumerable<object[]> MultipleBookmarks => new[]
        {
            new object[]
            {
                new[] { null, "illegalBookmark", FakeABookmark(123) },
                new[] { "illegalBookmark", FakeABookmark(123) }
            },
            new object[] { new[] { null, "illegalBookmark" }, new[] { "illegalBookmark" } },
            new object[] { new string[] { null }, new string[0] },
            new object[] { new[] { "illegalBookmark" }, new[] { "illegalBookmark" } },
            new object[]
            {
                new[] { FakeABookmark(123), FakeABookmark(234) }, new[] { FakeABookmark(123), FakeABookmark(234) }
            },
            new object[]
            {
                new[] { FakeABookmark(123), FakeABookmark(-234) }, new[] { FakeABookmark(123), FakeABookmark(-234) }
            }
        };

        [Theory]
        [MemberData(nameof(MultipleBookmarks))]
        public void ShouldCreateFromMultipleBookmarks(string[] bookmarks, string[] expectedValues)
        {
            var bookmark = Bookmarks.From(bookmarks);
            bookmark.Values.Should().BeEquivalentTo(expectedValues);
        }

        public class EqualsMethod
        {
            [Theory]
            [InlineData(new string[0], new string[0])]
            [InlineData(
                new[] { "bookmark-1", "bookmark-2", "bookmark-3" },
                new[] { "bookmark-1", "bookmark-2", "bookmark-3" })]
            [InlineData(
                new[] { null, "bookmark-1", "bookmark-2", "bookmark-3" },
                new[] { "bookmark-1", "bookmark-2", "bookmark-3", null })]
            [InlineData(
                new[] { null, "bookmark-1", "bookmark-2", "bookmark-3" },
                new[] { "bookmark-3", "bookmark-1", "bookmark-2", null })]
            public void ShouldBeEqual(string[] values1, string[] values2)
            {
                var bookmark1 = Bookmarks.From(values1);
                var bookmark2 = Bookmarks.From(values2);

                bookmark1.Should().Be(bookmark2);
            }
        }

        public class FromMethod
        {
            [Theory]
            [InlineData(new string[0], new string[0], new string[0])]
            [InlineData(new string[0], new[] { "bookmark-1" }, new[] { "bookmark-1" })]
            [InlineData(new[] { "bookmark-1" }, new[] { "bookmark-2" }, new[] { "bookmark-1", "bookmark-2" })]
            [InlineData(
                new[] { "bookmark-1", "bookmark-2" },
                new[] { "bookmark-2" },
                new[] { "bookmark-1", "bookmark-2" })]
            [InlineData(
                new[] { "bookmark-1", "bookmark-2" },
                new[] { "bookmark-2", "bookmark-3" },
                new[] { "bookmark-1", "bookmark-2", "bookmark-3" })]
            public void ShouldUnionValues(string[] values1, string[] values2, string[] values3)
            {
                var bookmark1 = Bookmarks.From(values1);
                var bookmark2 = Bookmarks.From(values2);
                var bookmark3 = Bookmarks.From(values3);

                Bookmarks.From(new[] { bookmark1, bookmark2 }).Should().Be(bookmark3);
            }
        }
    }
}
