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

using FluentAssertions;
using Moq;
using Moq.AutoMock;
using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.ObjectRegistry;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class NewBookmarkManagerHandlerTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<NewBookmarkManagerHandler>();

    [Fact]
    public async Task Registers_a_manager_seeded_with_the_initial_bookmarks_and_responds_with_its_id()
    {
        IBookmarkManager? manager = null;
        _autoMocker.GetMock<IRegistry>()
            .Setup(r => r.Register(It.IsAny<IBookmarkManager>()))
            .Returns<IBookmarkManager>(
                m =>
                {
                    manager = m;
                    return new RegistryObject<IBookmarkManager>("bm-1", m);
                });

        var handler = _autoMocker.CreateInstance<NewBookmarkManagerHandler>();
        var request = new NewBookmarkManagerRequest { InitialBookmarks = ["bm:1", "bm:2"] };

        await handler.ProcessAsync(request);

        _autoMocker.GetMock<IResponseWriter>()
            .Verify(w => w.WriteAsync(new BookmarkManagerResponse("bm-1")), Times.Once);

        Assert.NotNull(manager);
        var bookmarks = await manager!.GetBookmarksAsync(TestContext.Current.CancellationToken);
        bookmarks.Should().BeEquivalentTo("bm:1", "bm:2");
    }
}
