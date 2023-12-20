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
        var config = new BookmarkManagerConfig(
            new[] { "eg1", "eg2" },
            _ => Task.FromResult(Array.Empty<string>()),
            (_, _) => Task.CompletedTask);

        var bookmarkManager = new DefaultBookmarkManager(config);

        var bookmarks = await bookmarkManager.GetBookmarksAsync();

        bookmarks.Should().BeEquivalentTo("eg1", "eg2");
    }

    [Fact]
    public async Task ShouldReplaceBookmarks()
    {
        var config = new BookmarkManagerConfig(
            new[] { "eg1", "eg2" },
            _ => Task.FromResult(Array.Empty<string>()),
            (_, _) => Task.CompletedTask);

        var bookmarkManager = new DefaultBookmarkManager(config);

        await bookmarkManager.UpdateBookmarksAsync(new[] { "eg1", "eg2" }, new[] { "eg3", "eg4" });

        var bookmarks = await bookmarkManager.GetBookmarksAsync();

        bookmarks.Should().BeEquivalentTo("eg3", "eg4");
    }

    [Fact]
    public async Task ShouldOnlyReplaceReturnedBookmarks()
    {
        var config = new BookmarkManagerConfig(
            new[] { "eg1", "eg2" },
            _ => Task.FromResult(Array.Empty<string>()),
            (_, _) => Task.CompletedTask);

        var bookmarkManager = new DefaultBookmarkManager(config);

        await bookmarkManager.UpdateBookmarksAsync(new[] { "eg1" }, new[] { "eg3", "eg4" });

        var bookmarks = await bookmarkManager.GetBookmarksAsync();

        bookmarks.Should().BeEquivalentTo("eg2", "eg3", "eg4");
    }

    [Fact]
    public async Task ShouldCallNotifyBookmarksOnUpdate()
    {
        var notify = new Mock<Func<string[], CancellationToken, Task>>();
        notify
            .Setup(x => x(It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var config = new BookmarkManagerConfig(
            Array.Empty<string>(),
            _ => Task.FromResult(Array.Empty<string>()),
            notify.Object);

        var bookmarkManager = new DefaultBookmarkManager(config);

        await bookmarkManager.UpdateBookmarksAsync(Array.Empty<string>(), new[] { "eg3", "eg4" });

        notify.Verify(x => x(new[] { "eg3", "eg4" }, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ShouldReturnUnionOfProviderAndStoredValue()
    {
        var config = new BookmarkManagerConfig(
            new[] { "eg1" },
            _ => Task.FromResult(new[] { "eg2" }),
            (_, _) => Task.CompletedTask);

        var bookmarkManager = new DefaultBookmarkManager(config);

        var exists = await bookmarkManager.GetBookmarksAsync();
        exists.Should().BeEquivalentTo("eg1", "eg2");
    }

    [Fact]
    public async Task ShouldReturnUnionOfProviderWithNoInitial()
    {
        var config = new BookmarkManagerConfig(
            null,
            _ => Task.FromResult(new[] { "eg1" }),
            (_, _) => Task.CompletedTask);

        var bookmarkManager = new DefaultBookmarkManager(config);

        var exists = await bookmarkManager.GetBookmarksAsync();
        exists.Should().BeEquivalentTo("eg1");
    }

    [Fact]
    public async Task ShouldReturnDistinctUnionOfProviderAndStoredValue()
    {
        var config = new BookmarkManagerConfig(
            new[] { "eg1" },
            _ => Task.FromResult(new[] { "eg1" }),
            (_, _) => Task.CompletedTask);

        var bookmarkManager = new DefaultBookmarkManager(config);

        var exists = await bookmarkManager.GetBookmarksAsync();
        exists.Should().BeEquivalentTo("eg1");
    }
}
