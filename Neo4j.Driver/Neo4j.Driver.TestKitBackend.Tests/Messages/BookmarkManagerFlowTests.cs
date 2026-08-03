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
using Microsoft.Extensions.Logging;
using Moq;
using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Continuations;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.ObjectRegistry;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class BookmarkManagerFlowTests
{
    private readonly Mock<ICallbackExchanger> _callbacksMock = new();
    private readonly Mock<IResponseWriter> _responseWriterMock = new();

    private IBookmarkManager RegisterManager(NewBookmarkManagerRequest request)
    {
        IBookmarkManager? manager = null;
        var registryMock = new Mock<IRegistry>();
        registryMock
            .Setup(r => r.Register(It.IsAny<Func<string, IBookmarkManager>>()))
            .Returns<Func<string, IBookmarkManager>>(
                create =>
                {
                    manager = create("bm-1");
                    return new RegistryObject<IBookmarkManager>("bm-1", manager);
                });

        var handler = new NewBookmarkManagerHandler(
            registryMock.Object,
            _callbacksMock.Object,
            _responseWriterMock.Object,
            Mock.Of<ILogger>());

        handler.ProcessAsync(request).GetAwaiter().GetResult();

        Assert.NotNull(manager);
        return manager!;
    }

    [Fact]
    public async Task The_registered_manager_requests_a_supplier_callback_for_extra_bookmarks()
    {
        var manager = RegisterManager(new NewBookmarkManagerRequest { BookmarksSupplierRegistered = true });

        Func<string, ICallbackRequest>? capturedRequest = null;
        _callbacksMock
            .Setup(c => c.SendAsync<BookmarksSupplierCompletedRequest>(It.IsAny<Func<string, ICallbackRequest>>()))
            .Callback<Func<string, ICallbackRequest>>(f => capturedRequest = f)
            .ReturnsAsync(
                new BookmarksSupplierCompletedRequest { RequestId = "callback-1", Bookmarks = ["bm:s1", "bm:s2"] });

        var bookmarks = await manager.GetBookmarksAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(capturedRequest);
        var request = Assert.IsType<BookmarksSupplierRequest>(capturedRequest!("callback-1"));
        Assert.Equal("bm-1", request.BookmarkManagerId);
        bookmarks.Should().BeEquivalentTo("bm:s1", "bm:s2");
    }

    [Fact]
    public async Task The_registered_manager_requests_a_consumer_callback_with_the_new_bookmarks()
    {
        var manager = RegisterManager(new NewBookmarkManagerRequest { BookmarksConsumerRegistered = true });

        Func<string, ICallbackRequest>? capturedRequest = null;
        _callbacksMock
            .Setup(c => c.SendAsync<BookmarksConsumerCompletedRequest>(It.IsAny<Func<string, ICallbackRequest>>()))
            .Callback<Func<string, ICallbackRequest>>(f => capturedRequest = f)
            .ReturnsAsync(new BookmarksConsumerCompletedRequest { RequestId = "callback-1" });

        await manager.UpdateBookmarksAsync([], ["bm:new1", "bm:new2"], TestContext.Current.CancellationToken);

        Assert.NotNull(capturedRequest);
        var request = Assert.IsType<BookmarksConsumerRequest>(capturedRequest!("callback-1"));
        Assert.Equal("bm-1", request.BookmarkManagerId);
        request.Bookmarks.Should().BeEquivalentTo("bm:new1", "bm:new2");
    }
}
