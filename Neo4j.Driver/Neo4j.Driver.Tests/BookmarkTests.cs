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

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using FluentAssertions;
using Neo4j.Driver.Internal;
using Neo4j.Driver;
using Xunit;
using static Neo4j.Driver.Tests.SessionTests;

namespace Neo4j.Driver.Tests
{
    public class BookmarkTests
    {
        public class ConstructorMethod
        {
            public static IEnumerable<object[]> MultipleBookmarks => new[]
            {
                new object[]
                {
                    new[] {null, "illegalBookmark", FakeABookmark(123)}, new[] {"illegalBookmark", FakeABookmark(123)}
                },
                new object[] {new[] {null, "illegalBookmark"}, new[] {"illegalBookmark"}},
                new object[] {new string[] {null}, new string[0]},
                new object[] {new[] {"illegalBookmark"}, new[] {"illegalBookmark"}},
                new object[]
                    {new[] {FakeABookmark(123), FakeABookmark(234)}, new[] {FakeABookmark(123), FakeABookmark(234)}},
                new object[]
                    {new[] {FakeABookmark(123), FakeABookmark(-234)}, new[] {FakeABookmark(123), FakeABookmark(-234)}},
            };

            [Theory, MemberData(nameof(MultipleBookmarks))]
            public void ShouldCreateFromMultipleBookmarks(string[] bookmarks, string[] expectedValues)
            {
                var bookmark = Bookmark.From(bookmarks);
                bookmark.Values.Should().BeEquivalentTo(expectedValues);

                var parameters = bookmark.AsBeginTransactionParameters();
                if (expectedValues.Length > 0)
                {
                    parameters.Should().ContainKey("bookmarks").WhichValue.Should().BeEquivalentTo(expectedValues);
                }
                else
                {
                    parameters.Should().BeNull();
                }
            }

            public class Equals
            {
                [Theory]
                [InlineData(new string[0], new string[0])]
                [InlineData(new[] {"bookmark-1", "bookmark-2", "bookmark-3"},
                    new[] {"bookmark-1", "bookmark-2", "bookmark-3"})]
                [InlineData(new[] {null, "bookmark-1", "bookmark-2", "bookmark-3"},
                    new[] {"bookmark-1", "bookmark-2", "bookmark-3", null})]
                [InlineData(new[] {null, "bookmark-1", "bookmark-2", "bookmark-3"},
                    new[] {"bookmark-3", "bookmark-1", "bookmark-2", null})]
                public void ShouldBeEqual(string[] values1, string[] values2)
                {
                    var bookmark1 = Bookmark.From(values1);
                    var bookmark2 = Bookmark.From(values2);

                    bookmark1.Should().Be(bookmark2);
                }
            }

            public class From
            {
                [Theory]
                [InlineData(new string[0], new string[0], new string[0])]
                [InlineData(new string[0], new[] {"bookmark-1"}, new[] {"bookmark-1"})]
                [InlineData(new[] {"bookmark-1"}, new[] {"bookmark-2"}, new[] {"bookmark-1", "bookmark-2"})]
                [InlineData(new[] {"bookmark-1", "bookmark-2"}, new[] {"bookmark-2"},
                    new[] {"bookmark-1", "bookmark-2"})]
                [InlineData(new[] {"bookmark-1", "bookmark-2"}, new[] {"bookmark-2", "bookmark-3"},
                    new[] {"bookmark-1", "bookmark-2", "bookmark-3"})]
                public void ShouldUnionValues(string[] values1, string[] values2, string[] values3)
                {
                    var bookmark1 = Bookmark.From(values1);
                    var bookmark2 = Bookmark.From(values2);
                    var bookmark3 = Bookmark.From(values3);

                    Bookmark.From(new[] {bookmark1, bookmark2}).Should().Be(bookmark3);
                }
            }
        }
    }
}