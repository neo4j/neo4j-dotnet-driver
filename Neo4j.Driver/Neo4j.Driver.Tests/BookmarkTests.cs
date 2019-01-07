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
using FluentAssertions;
using Neo4j.Driver.Internal;
using Neo4j.Driver.V1;
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
                new object[] {new[] {null, "illegalBookmark", FakeABookmark(123)}, FakeABookmark(123), false},
                new object[] {new[] {null, "illegalBookmark"}, null, true},
                new object[] {new string[] {null}, null, true},
                new object[] {new[] {"illegalBookmark"}, null, true},
                new object[] {new[] {FakeABookmark(123), FakeABookmark(234)}, FakeABookmark(234), false},
                new object[] {new[] {FakeABookmark(123), FakeABookmark(-234)}, FakeABookmark(123), false},
            };

            [Theory, MemberData(nameof(MultipleBookmarks))]
            public void ShouldCreateFromMultipleBookmarks(IEnumerable<string> bookmarks, string maxBookmark,
                bool isEmpty)
            {
                var bookmark = Bookmark.From(bookmarks);
                bookmark.MaxBookmark.Should().Be(maxBookmark);
                bookmark.IsEmpty().Should().Be(isEmpty);
                var parameters = bookmark.AsBeginTransactionParameters();
                if (isEmpty)
                {
                    parameters.Should().BeNull();
                }
                else
                {
                    parameters["bookmark"].Should().Be(maxBookmark);
                    parameters["bookmarks"].ValueAs<List<string>>().Should().Contain(bookmarks);
                }
            }

            public static IEnumerable<object[]> SingleBookmark => new[]
            {
                new object[] {null, null, true},
                new object[] {"illegalBookmark", null, true},
                new object[] {FakeABookmark(-234), null, true},
                new object[] {FakeABookmark(123), FakeABookmark(123), false},
            };

            [Theory, MemberData(nameof(SingleBookmark))]
            public void ShouldCreateFromSingleBookmark(string aBookmark, string maxBookmark, bool isEmpty)
            {
                var bookmark = Bookmark.From(aBookmark);
                bookmark.MaxBookmark.Should().Be(maxBookmark);
                bookmark.IsEmpty().Should().Be(isEmpty);
                var parameters = bookmark.AsBeginTransactionParameters();
                if (isEmpty)
                {
                    parameters.Should().BeNull();
                }
                else
                {
                    parameters["bookmark"].Should().Be(maxBookmark);
                    var bookmarks = parameters["bookmarks"].ValueAs<List<string>>();
                    bookmarks.Single().Should().Be(aBookmark);
                }
            }
        }
    }
}
