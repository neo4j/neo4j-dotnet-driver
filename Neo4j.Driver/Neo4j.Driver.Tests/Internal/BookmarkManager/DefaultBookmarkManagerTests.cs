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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;

namespace Neo4j.Driver.Internal.BookmarkManager;

public class DefaultBookmarkManagerTests
{
    [Fact]
    public async Task ShouldReturnBookmarks()
    {
        var initialBookmarks = new Dictionary<string, IEnumerable<string>>
        {
            ["example"] = new[] { "eg1", "eg2" }
        };

        var config = new BookmarkManagerConfig(
            initialBookmarks,
            (_, _) => Task.FromResult(Array.Empty<string>()),
            (_, _, _) => Task.CompletedTask);

        var bookmarkManager = new DefaultBookmarkManager(config);

        var bookmarks = await bookmarkManager.GetBookmarksAsync("example");

        bookmarks.Should().BeEquivalentTo("eg1", "eg2");
    }

    [Fact]
    public async Task ShouldReplaceBookmarks()
    {
        var initialBookmarks = new Dictionary<string, IEnumerable<string>>
        {
            ["example"] = new[] { "eg1", "eg2" }
        };

        var config = new BookmarkManagerConfig(
            initialBookmarks,
            (_, _) => Task.FromResult(Array.Empty<string>()),
            (_, _, _) => Task.CompletedTask);

        var bookmarkManager = new DefaultBookmarkManager(config);

        await bookmarkManager.UpdateBookmarksAsync("example", new[] { "eg1", "eg2" }, new[] { "eg3", "eg4" });

        var bookmarks = await bookmarkManager.GetBookmarksAsync("example");

        bookmarks.Should().BeEquivalentTo("eg3", "eg4");
    }

    [Fact]
    public async Task ShouldOnlyReplaceReturnedBookmarks()
    {
        var initialBookmarks = new Dictionary<string, IEnumerable<string>>
        {
            ["example"] = new[] { "eg1", "eg2" }
        };

        var config = new BookmarkManagerConfig(
            initialBookmarks,
            (_, _) => Task.FromResult(Array.Empty<string>()),
            (_, _, _) => Task.CompletedTask);

        var bookmarkManager = new DefaultBookmarkManager(config);

        await bookmarkManager.UpdateBookmarksAsync("example", new[] { "eg1" }, new[] { "eg3", "eg4" });

        var bookmarks = await bookmarkManager.GetBookmarksAsync("example");

        bookmarks.Should().BeEquivalentTo("eg2", "eg3", "eg4");
    }

    [Fact]
    public async Task ShouldCallNotifyBookmarksOnUpdate()
    {
        var notify = new Mock<Func<string, string[], CancellationToken, Task>>();
        notify
            .Setup(x => x(It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var config = new BookmarkManagerConfig(
            new Dictionary<string, IEnumerable<string>>(),
            (_, _) => Task.FromResult(Array.Empty<string>()),
            notify.Object);

        var bookmarkManager = new DefaultBookmarkManager(config);

        await bookmarkManager.UpdateBookmarksAsync("example", Array.Empty<string>(), new[] { "eg3", "eg4" });

        notify.Verify(x => x("example", new[] { "eg3", "eg4" }, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ShouldReturnEmptyIfDatabaseNotSet()
    {
        var initialBookmarks = new Dictionary<string, IEnumerable<string>>
        {
            ["notReturned"] = new[] { "eg1", "eg2" }
        };

        var config = new BookmarkManagerConfig(
            initialBookmarks,
            (_, _) => Task.FromResult(Array.Empty<string>()),
            (_, _, _) => Task.CompletedTask);

        var bookmarkManager = new DefaultBookmarkManager(config);

        var exists = await bookmarkManager.GetBookmarksAsync("example");
        exists.Should().BeEquivalentTo(Array.Empty<string>());
    }

    [Fact]
    public async Task ShouldAddBookmarksWithNoDatabase()
    {
        var initialBookmarks = new Dictionary<string, IEnumerable<string>>
        {
            ["notReturned"] = new[] { "eg1", "eg2" }
        };

        var config = new BookmarkManagerConfig(
            initialBookmarks,
            (_, _) => Task.FromResult(Array.Empty<string>()),
            (_, _, _) => Task.CompletedTask);

        var bookmarkManager = new DefaultBookmarkManager(config);

        var exists = await bookmarkManager.GetBookmarksAsync("example");
        exists.Should().BeEquivalentTo(Array.Empty<string>());

        await bookmarkManager.UpdateBookmarksAsync("example", new[] { "eg1", "eg2" }, new[] { "eg3" });

        var updated = await bookmarkManager.GetBookmarksAsync("example");
        updated.Should().BeEquivalentTo("eg3");

        // assert only correct db's bookmarks updated.
        var unaffected = await bookmarkManager.GetBookmarksAsync("notReturned");
        unaffected.Should().BeEquivalentTo("eg1", "eg2");
    }

    [Fact]
    public async Task ShouldReturnUnionOfProviderAndStoredValue()
    {
        var initialBookmarks = new Dictionary<string, IEnumerable<string>>
        {
            ["example"] = new[] { "eg1" }
        };

        var config = new BookmarkManagerConfig(
            initialBookmarks,
            (_, _) => Task.FromResult(new[] { "eg2" }),
            (_, _, _) => Task.CompletedTask);

        var bookmarkManager = new DefaultBookmarkManager(config);

        var exists = await bookmarkManager.GetBookmarksAsync("example");
        exists.Should().BeEquivalentTo("eg1", "eg2");
    }

    [Fact]
    public async Task ShouldReturnUnionOfProviderWithNoInitial()
    {
        var config = new BookmarkManagerConfig(
            new Dictionary<string, IEnumerable<string>>(),
            (_, _) => Task.FromResult(new[] { "eg1" }),
            (_, _, _) => Task.CompletedTask);

        var bookmarkManager = new DefaultBookmarkManager(config);

        var exists = await bookmarkManager.GetBookmarksAsync("example");
        exists.Should().BeEquivalentTo("eg1");
    }

    [Fact]
    public async Task ShouldReturnDistinctUnionOfProviderAndStoredValue()
    {
        var initialBookmarks = new Dictionary<string, IEnumerable<string>>
        {
            ["example"] = new[] { "eg1" }
        };

        var config = new BookmarkManagerConfig(
            initialBookmarks,
            (_, _) => Task.FromResult(new[] { "eg1" }),
            (_, _, _) => Task.CompletedTask);

        var bookmarkManager = new DefaultBookmarkManager(config);

        var exists = await bookmarkManager.GetBookmarksAsync("example");
        exists.Should().BeEquivalentTo("eg1");
    }

    [Fact]
    public async Task ShouldReturnDistinctUnionOfAllBookmarksForKnownDatabasesNoSpecifiedDb()
    {
        var initialBookmarks = new Dictionary<string, IEnumerable<string>>
        {
            ["example"] = new[] { "eg1" },
            ["example2"] = new[] { "eg2" }
        };

        var mock = new Mock<Func<string, CancellationToken, Task<string[]>>>();
        mock.Setup(x => x.Invoke(It.IsNotNull<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception());
        mock.Setup(x => x.Invoke(null, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { "eg1", "provider2" });

        var config = new BookmarkManagerConfig(
            initialBookmarks,
            mock.Object,
            (_, _, _) => Task.CompletedTask);

        var bookmarkManager = new DefaultBookmarkManager(config);

        var exists = await bookmarkManager.GetAllBookmarksAsync();

        mock.Verify(x => x.Invoke(null, It.IsAny<CancellationToken>()), Times.Once);

        exists.Should().BeEquivalentTo("eg1", "provider2", "eg2");
    }

    [Fact]
    public async Task ShouldReturnDistinctUnionOfAllBookmarksForKnownDatabasesWithSpecifiedDb()
    {
        var initialBookmarks = new Dictionary<string, IEnumerable<string>>
        {
            ["INC"] = new[] { "eg1" },
            ["EXC"] = new[] { "eg2" }
        };

        var mock = new Mock<Func<string, CancellationToken, Task<string[]>>>();
        mock.Setup(x => x.Invoke(null, It.IsAny<CancellationToken>())).Throws(new Exception());
        mock.Setup(x => x.Invoke("INC", It.IsAny<CancellationToken>())).ReturnsAsync(new[] { "eg1", "provider3" });

        var config = new BookmarkManagerConfig(
            initialBookmarks,
            mock.Object,
            (_, _, _) => Task.CompletedTask);

        var bookmarkManager = new DefaultBookmarkManager(config);

        var exists = await bookmarkManager.GetBookmarksAsync("INC");
        exists.Should().BeEquivalentTo("eg1", "provider3");
    }

    [Fact]
    public async Task ShouldForgetAllDatabases()
    {
        var initial = new Dictionary<string, IEnumerable<string>>
        {
            ["a"] = new[] { "eg1" },
            ["b"] = new[] { "eg2" }
        };

        var bookmarkManager = new DefaultBookmarkManager(new BookmarkManagerConfig(initial));

        await bookmarkManager.ForgetAsync();

        var exists = await bookmarkManager.GetAllBookmarksAsync();
        exists.Should().BeEquivalentTo(Array.Empty<string>());
    }

    [Fact]
    public async Task ShouldForgetSpecifiedDatabase()
    {
        var initial = new Dictionary<string, IEnumerable<string>>
        {
            ["a"] = new[] { "eg1" },
            ["b"] = new[] { "eg2" }
        };

        var bookmarkManager = new DefaultBookmarkManager(new BookmarkManagerConfig(initial));

        await bookmarkManager.ForgetAsync(new[] { "a" });

        var exists = await bookmarkManager.GetAllBookmarksAsync();
        exists.Should().BeEquivalentTo("eg2");
    }

    [Fact]
    public async Task ShouldForgetSpecifiedDatabases()
    {
        var initial = new Dictionary<string, IEnumerable<string>>
        {
            ["a"] = new[] { "eg1" },
            ["b"] = new[] { "eg2" },
            ["c"] = new[] { "eg3" }
        };

        var bookmarkManager = new DefaultBookmarkManager(new BookmarkManagerConfig(initial));

        await bookmarkManager.ForgetAsync(new[] { "a", "b" });

        var exists = await bookmarkManager.GetAllBookmarksAsync();
        exists.Should().BeEquivalentTo("eg3");
    }
}
