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

using Microsoft.Extensions.Logging;
using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Continuations;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.ObjectRegistry;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal record NewBookmarkManagerRequest : IProtocolMessage
{
    public string[]? InitialBookmarks { get; init; }
    public bool BookmarksSupplierRegistered { get; init; }
    public bool BookmarksConsumerRegistered { get; init; }
}

internal record BookmarkManagerResponse(string Id) : IProtocolMessage;

internal class NewBookmarkManagerHandler : MessageHandler<NewBookmarkManagerRequest>
{
    private readonly IRegistry _registry;
    private readonly ICallbackExchange _callbacks;
    private readonly IResponseWriter _responseWriter;
    private readonly ILogger _logger;

    public NewBookmarkManagerHandler(
        IRegistry registry,
        ICallbackExchange callbacks,
        IResponseWriter responseWriter,
        ILogger logger)
    {
        _registry = registry;
        _callbacks = callbacks;
        _responseWriter = responseWriter;
        _logger = logger;
    }

    public override async Task ProcessAsync(NewBookmarkManagerRequest message)
    {
        var managerId = "";

        Func<CancellationToken, Task<string[]>>? supplier = message.BookmarksSupplierRegistered
            ? _ => SupplyBookmarksAsync(managerId)
            : null;

        Func<string[], CancellationToken, Task>? consumer = message.BookmarksConsumerRegistered
            ? (bookmarks, _) => ConsumeBookmarksAsync(managerId, bookmarks)
            : null;

        var manager = GraphDatabase.BookmarkManagerFactory.NewBookmarkManager(
            new BookmarkManagerConfig(message.InitialBookmarks, supplier, consumer));

        var registered = _registry.Register(manager);
        managerId = registered.Id;

        _logger.LogDebug("Created bookmark manager with id '{Id}'", registered.Id);
        await _responseWriter.WriteAsync(new BookmarkManagerResponse(registered.Id));
    }

    private async Task<string[]> SupplyBookmarksAsync(string managerId)
    {
        var completion = await _callbacks.SendAsync<BookmarksSupplierCompletedRequest>(
            id => new BookmarksSupplierRequest(id, managerId));

        return completion.Bookmarks;
    }

    private async Task ConsumeBookmarksAsync(string managerId, string[] bookmarks)
    {
        await _callbacks.SendAsync<BookmarksConsumerCompletedRequest>(
            id => new BookmarksConsumerRequest(id, managerId, bookmarks));
    }
}
