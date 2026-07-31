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
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.ObjectRegistry;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

// The manager handler and the completed handlers only make sense together — this pins the
// callback handshakes between them via a real IContinuationCoordinator, playing the roles of
// the driver (invoking the registered manager) and of the detached operation whose response
// slot the callback borrows.
public class BookmarkManagerFlowTests
{
    private record TerminalResponse(string Tag) : IProtocolMessage;

    private readonly ContinuationCoordinator _coordinator = new();
    private readonly Mock<IResponseWriter> _responseWriterMock = new();

    private IBookmarkManager RegisterManager(NewBookmarkManagerRequest request)
    {
        IBookmarkManager? manager = null;
        var registryMock = new Mock<IRegistry>();
        registryMock
            .Setup(r => r.Register(It.IsAny<IBookmarkManager>()))
            .Returns<IBookmarkManager>(
                m =>
                {
                    manager = m;
                    return new RegistryObject<IBookmarkManager>("bm-1", m);
                });

        var handler = new NewBookmarkManagerHandler(
            registryMock.Object,
            _coordinator,
            _responseWriterMock.Object,
            Mock.Of<ILogger>());

        handler.ProcessAsync(request).GetAwaiter().GetResult();

        Assert.NotNull(manager);
        return manager!;
    }

    [Fact]
    public async Task The_registered_manager_round_trips_a_supplier_callback_for_extra_bookmarks()
    {
        var manager = RegisterManager(new NewBookmarkManagerRequest { BookmarksSupplierRegistered = true });

        // Play the detached operation whose response slot the callback borrows...
        var openRequestTask = _coordinator.WaitForNextResponseAsync();

        // ...and the driver asking the manager for bookmarks mid-operation.
        var bookmarksTask = manager.GetBookmarksAsync(TestContext.Current.CancellationToken);

        var callbackRequest = Assert.IsType<BookmarksSupplierRequest>(await WithTimeoutAsync(openRequestTask));
        Assert.Equal("bm-1", callbackRequest.BookmarkManagerId);

        var completedHandler = new CallbackCompletedHandler<BookmarksSupplierCompletedRequest>(
            _coordinator,
            _responseWriterMock.Object);
        var completedTask = completedHandler.ProcessAsync(
            new BookmarksSupplierCompletedRequest
            {
                RequestId = callbackRequest.Id,
                Bookmarks = ["bm:s1", "bm:s2"]
            });

        var bookmarks = await WithTimeoutAsync(bookmarksTask);
        bookmarks.Should().BeEquivalentTo("bm:s1", "bm:s2");

        // The resumed operation eventually produces the terminal response; the completed handler
        // is the one holding the response slot, so it writes it.
        _coordinator.CompleteNextResponse(new TerminalResponse("result"));
        await WithTimeoutAsync(completedTask);

        _responseWriterMock.Verify(w => w.WriteAsync(new TerminalResponse("result")), Times.Once);
    }

    [Fact]
    public async Task The_registered_manager_round_trips_a_consumer_callback_with_the_new_bookmarks()
    {
        var manager = RegisterManager(new NewBookmarkManagerRequest { BookmarksConsumerRegistered = true });

        var openRequestTask = _coordinator.WaitForNextResponseAsync();

        var updateTask = manager.UpdateBookmarksAsync(
            [],
            ["bm:new1", "bm:new2"],
            TestContext.Current.CancellationToken);

        var callbackRequest = Assert.IsType<BookmarksConsumerRequest>(await WithTimeoutAsync(openRequestTask));
        Assert.Equal("bm-1", callbackRequest.BookmarkManagerId);
        callbackRequest.Bookmarks.Should().BeEquivalentTo("bm:new1", "bm:new2");

        var completedHandler = new CallbackCompletedHandler<BookmarksConsumerCompletedRequest>(
            _coordinator,
            _responseWriterMock.Object);
        var completedTask = completedHandler.ProcessAsync(
            new BookmarksConsumerCompletedRequest { RequestId = callbackRequest.Id });

        await WithTimeoutAsync(updateTask);

        _coordinator.CompleteNextResponse(new TerminalResponse("result"));
        await WithTimeoutAsync(completedTask);

        _responseWriterMock.Verify(w => w.WriteAsync(new TerminalResponse("result")), Times.Once);
    }

    private static async Task<T> WithTimeoutAsync<T>(Task<T> task)
    {
        var completed = await Task.WhenAny(
            task,
            Task.Delay(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));

        Assert.Same(task, completed);
        return await task;
    }

    private static async Task WithTimeoutAsync(Task task)
    {
        var completed = await Task.WhenAny(
            task,
            Task.Delay(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));

        Assert.Same(task, completed);
        await task;
    }
}
