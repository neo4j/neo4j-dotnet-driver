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
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Expectations;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.ObjectStorage;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class BookmarkManagerFlowTests
{
    private readonly Mock<IOutboundRoundTrip> _roundTripMock = new();
    private readonly Mock<IResponseWriter> _responseWriterMock = new();

    private IBookmarkManager StoreManager(NewBookmarkManagerRequest request)
    {
        IBookmarkManager? manager = null;
        var objectStoreMock = new Mock<IObjectStore>();
        objectStoreMock
            .Setup(r => r.Store(It.IsAny<Func<string, IBookmarkManager>>()))
            .Returns<Func<string, IBookmarkManager>>(
                create =>
                {
                    manager = create("bm-1");
                    return "bm-1";
                });

        var handler = new NewBookmarkManagerHandler(
            objectStoreMock.Object,
            _roundTripMock.Object,
            _responseWriterMock.Object,
            Mock.Of<ILogger>());

        handler.ProcessAsync(request).GetAwaiter().GetResult();

        manager.Should().NotBeNull();
        return manager!;
    }

    [Fact]
    public async Task The_stored_manager_requests_a_supplier_callback_for_extra_bookmarks()
    {
        var manager = StoreManager(new NewBookmarkManagerRequest { BookmarksSupplierRegistered = true });

        IProtocolMessage? capturedRequest = null;
        _roundTripMock
            .Setup(r => r.SendExpectingAsync<string[]>(It.IsAny<IProtocolMessage>()))
            .Callback<IProtocolMessage>(request => capturedRequest = request)
            .ReturnsAsync(["bm:s1", "bm:s2"]);

        var bookmarks = await manager.GetBookmarksAsync(TestContext.Current.CancellationToken);

        var request = capturedRequest.Should().BeOfType<BookmarksSupplierRequest>().Subject;
        request.BookmarkManagerId.Should().Be("bm-1");
        bookmarks.Should().BeEquivalentTo("bm:s1", "bm:s2");
    }

    [Fact]
    public async Task The_stored_manager_requests_a_consumer_callback_with_the_new_bookmarks()
    {
        var manager = StoreManager(new NewBookmarkManagerRequest { BookmarksConsumerRegistered = true });

        IProtocolMessage? capturedRequest = null;
        _roundTripMock
            .Setup(r => r.SendExpectingAsync<bool>(It.IsAny<IProtocolMessage>()))
            .Callback<IProtocolMessage>(request => capturedRequest = request)
            .ReturnsAsync(true);

        await manager.UpdateBookmarksAsync([], ["bm:new1", "bm:new2"], TestContext.Current.CancellationToken);

        var request = capturedRequest.Should().BeOfType<BookmarksConsumerRequest>().Subject;
        request.BookmarkManagerId.Should().Be("bm-1");
        request.Bookmarks.Should().BeEquivalentTo("bm:new1", "bm:new2");
    }

    [Fact]
    public void BookmarksSupplierCompleted_fulfils_the_expectation_with_the_bookmarks()
    {
        var expectationsMock = new Mock<IExpectationStore>();
        var handler = new BookmarksSupplierCompletedHandler(expectationsMock.Object);
        var message = new BookmarksSupplierCompleted { RequestId = "callback-1", Bookmarks = ["bm:1", "bm:2"] };

        handler.ProcessAsync(message);

        expectationsMock.Verify(e => e.Fulfil("callback-1", message.Bookmarks), Times.Once);
    }

    [Fact]
    public void BookmarksConsumerCompleted_fulfils_the_expectation()
    {
        var expectationsMock = new Mock<IExpectationStore>();
        var handler = new BookmarksConsumerCompletedHandler(expectationsMock.Object);
        var message = new BookmarksConsumerCompleted { RequestId = "callback-1" };

        handler.ProcessAsync(message);

        expectationsMock.Verify(e => e.Fulfil("callback-1", true), Times.Once);
    }
}
