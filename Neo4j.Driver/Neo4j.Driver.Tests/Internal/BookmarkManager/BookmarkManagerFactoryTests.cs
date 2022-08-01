using System;
using System.Collections.Generic;
using Xunit;
using FluentAssertions;

namespace Neo4j.Driver.Internal.BookmarkManager;

public class BookmarkManagerFactoryTests
{
    [Fact]
    public void ShouldReturnNewBookmarkManager()
    {
        var factory = new BookmarkManagerFactory();

        var config = new BookmarkManagerConfig(
            new Dictionary<string, IEnumerable<string>>(),
            _ => Array.Empty<string>(),
            (_, _) => {});

        var bookmarkManager = factory.NewBookmarkManager(config);

        bookmarkManager.Should().BeAssignableTo<DefaultBookmarkManager>();
    }
    [Fact]
    public void ShouldReturnNoOpBookmarkManagerOnNullConfig()
    {
        var factory = new BookmarkManagerFactory();

        var bookmarkManager = factory.NewBookmarkManager(null);

        bookmarkManager.Should().BeAssignableTo<NoOpBookmarkManager>();
    }
}