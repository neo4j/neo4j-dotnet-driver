// Copyright (c) 2002-2022 "Neo4j,"
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
using Xunit;

namespace Neo4j.Driver.Internal.BookmarkManager;

public class DefaultBookmarkManagerTests
{
    [Fact]
    public void ShouldReturnBookmarks()
    {
        var initialBookmarks = new Dictionary<string, IEnumerable<string>>()
        {
            ["example"] = new[] {"eg1", "eg2"}
        };
        var config = new BookmarkManagerConfig(
            initialBookmarks,
            _ => Array.Empty<string>(),
            (_, _) => { });

        var bookmarkManager = new DefaultBookmarkManager(config);

        var bookmarks = bookmarkManager.GetBookmarks("example");

        bookmarks.Should().Be(Bookmarks.From("eg1", "eg2"));
    }

    [Fact]
    public void ShouldReplaceBookmarks()
    {
        var initialBookmarks = new Dictionary<string, IEnumerable<string>>()
        {
            ["example"] = new[] {"eg1", "eg2"}
        };
        var config = new BookmarkManagerConfig(
            initialBookmarks,
            _ => Array.Empty<string>(),
            (_, _) => { });

        var bookmarkManager = new DefaultBookmarkManager(config);

        bookmarkManager.UpdateBookmarks("example", new[] {"eg1", "eg2"}, new[] {"eg3", "eg4"});

        var bookmarks = bookmarkManager.GetBookmarks("example");

        bookmarks.Should().Be(Bookmarks.From("eg3", "eg4"));
    }

    [Fact]
    public void ShouldOnlyReplaceReturnedBookmarks()
    {
        var initialBookmarks = new Dictionary<string, IEnumerable<string>>()
        {
            ["example"] = new[] {"eg1", "eg2"}
        };
        var config = new BookmarkManagerConfig(
            initialBookmarks,
            _ => Array.Empty<string>(),
            (_, _) => { });

        var bookmarkManager = new DefaultBookmarkManager(config);

        bookmarkManager.UpdateBookmarks("example", new[] {"eg1"}, new[] {"eg3", "eg4"});

        var bookmarks = bookmarkManager.GetBookmarks("example");

        bookmarks.Should().Be(Bookmarks.From("eg2", "eg3", "eg4"));
    }

    [Fact]
    public void ShouldCallNotifyBookmarksOnUpdate()
    {
        var notify = new Mock<Action<string, string[]>>();

        var config = new BookmarkManagerConfig(
            new Dictionary<string, IEnumerable<string>>(),
            _ => Array.Empty<string>(),
            notify.Object);

        var bookmarkManager = new DefaultBookmarkManager(config);

        bookmarkManager.UpdateBookmarks("example", Array.Empty<string>(), new[] {"eg3", "eg4"});

        notify.Verify(x => x("example", new[] {"eg3", "eg4"}), Times.Once);
    }

    [Fact]
    public void ShouldReturnEmptyIfDatabaseNotSet()
    {
        var initialBookmarks = new Dictionary<string, IEnumerable<string>>()
        {
            ["notReturned"] = new[] { "eg1", "eg2" }
        };

        var config = new BookmarkManagerConfig(
            initialBookmarks,
            _ => Array.Empty<string>(),
            (_, _) => { });

        var bookmarkManager = new DefaultBookmarkManager(config);

        var exists = bookmarkManager.GetBookmarks("example");
        exists.Should().Be(Bookmarks.Empty);
    }

    [Fact]
    public void ShouldAddBookmarksWithNoDatabase()
    {
        var initialBookmarks = new Dictionary<string, IEnumerable<string>>()
        {
            ["notReturned"] = new[] { "eg1", "eg2" }
        };
        var config = new BookmarkManagerConfig(
            initialBookmarks,
            _ => Array.Empty<string>(),
            (_, _) => { });

        var bookmarkManager = new DefaultBookmarkManager(config);

        var exists = bookmarkManager.GetBookmarks("example");
        exists.Should().Be(Bookmarks.Empty);

        bookmarkManager.UpdateBookmarks("example", new []{"eg1", "eg2"}, new[] {"eg3"});

        var updated = bookmarkManager.GetBookmarks("example");
        updated.Should().Be(Bookmarks.From("eg3"));

        // assert only correct db's bookmarks updated.
        var unaffected = bookmarkManager.GetBookmarks("notReturned");
        unaffected.Should().Be(Bookmarks.From("eg1", "eg2"));
    }

    [Fact]
    public void ShouldReturnUnionOfProviderAndStoredValue()
    {
        var initialBookmarks = new Dictionary<string, IEnumerable<string>>()
        {
            ["example"] = new[] { "eg1" }
        };

        var config = new BookmarkManagerConfig(
            initialBookmarks,
            _ => new[] { "eg2" },
            (_, _) => { });

        var bookmarkManager = new DefaultBookmarkManager(config);

        var exists = bookmarkManager.GetBookmarks("example");
        exists.Should().Be(Bookmarks.From("eg1", "eg2"));
    }

    [Fact]
    public void ShouldReturnUnionOfProviderWithNoInitial()
    {
        var config = new BookmarkManagerConfig(
            new Dictionary<string, IEnumerable<string>>(),
            _ => new[] { "eg1" },
            (_, _) => { });

        var bookmarkManager = new DefaultBookmarkManager(config);

        var exists = bookmarkManager.GetBookmarks("example");
        exists.Should().Be(Bookmarks.From("eg1"));
    }

    [Fact]
    public void ShouldReturnDistinctUnionOfProviderAndStoredValue()
    {
        var initialBookmarks = new Dictionary<string, IEnumerable<string>>()
        {
            ["example"] = new[] { "eg1" }
        };

        var config = new BookmarkManagerConfig(
            initialBookmarks,
            _ => new[] { "eg1" },
            (_, _) => { });

        var bookmarkManager = new DefaultBookmarkManager(config);

        var exists = bookmarkManager.GetBookmarks("example");
        exists.Should().Be(Bookmarks.From("eg1"));
    }

    [Fact]
    public void ShouldReturnDistinctUnionOfAllBookmarksForKnownDatabases()
    {
        var initialBookmarks = new Dictionary<string, IEnumerable<string>>()
        {
            ["example"] = new[] { "eg1" },
            ["example2"] = new[] { "eg2" }
        };

        string[] Provider(string db)
        {
            if (db == "example3")
                return new[] {"provider3"};
            if (db == "example")
                return new[] {"eg1", "provider1"};
            return Array.Empty<string>();
        }

        var config = new BookmarkManagerConfig(
            initialBookmarks,
            Provider,
            (_, _) => { });

        var bookmarkManager = new DefaultBookmarkManager(config);

        var exists = bookmarkManager.GetAllBookmarks();
        exists.Should().BeEquivalentTo(new [] {"eg1", "provider1", "eg2"});
    }

    [Fact]
    public void ShouldReturnDistinctUnionOfAllBookmarksAndRunGetForSpecifiedDbs()
    {
        var initialBookmarks = new Dictionary<string, IEnumerable<string>>()
        {
            ["example"] = new[] { "eg1" },
            ["example2"] = new[] { "eg2" }
        };

        string[] Provider(string db)
        {
            if (db == "example3")
                return new[] { "eg2", "provider3" };
            return Array.Empty<string>();
        }

        var config = new BookmarkManagerConfig(
            initialBookmarks,
            Provider,
            (_, _) => { });

        var bookmarkManager = new DefaultBookmarkManager(config);

        var exists = bookmarkManager.GetAllBookmarks("example3");
        exists.Should().BeEquivalentTo("eg1", "eg2", "provider3");
    }
}