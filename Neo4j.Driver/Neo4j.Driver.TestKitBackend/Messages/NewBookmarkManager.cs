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
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Expectations;
using Neo4j.Driver.TestKitBackend.ObjectStorage;

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
    private readonly IObjectStore _objectStore;
    private readonly IOutboundRoundTrip _roundTrip;
    private readonly IResponseWriter _responseWriter;
    private readonly ILogger _logger;

    public NewBookmarkManagerHandler(
        IObjectStore objectStore,
        IOutboundRoundTrip roundTrip,
        IResponseWriter responseWriter,
        ILogger logger)
    {
        _objectStore = objectStore;
        _roundTrip = roundTrip;
        _responseWriter = responseWriter;
        _logger = logger;
    }

    public override async Task ProcessAsync(NewBookmarkManagerRequest message)
    {
        var stored = _objectStore.Store(id => CreateStoredManager(message, id));
        _logger.LogDebug("Created bookmark manager with id '{Id}'", stored.Id);
        await _responseWriter.WriteAsync(new BookmarkManagerResponse(stored.Id));
    }

    private IBookmarkManager CreateStoredManager(NewBookmarkManagerRequest message, string managerId)
    {
        Task<string[]> SupplyFromManager(CancellationToken _) => SupplyBookmarksAsync(managerId);
        Task ConsumeFromManager(string[] bookmarks, CancellationToken _) => ConsumeBookmarksAsync(managerId, bookmarks);

        Func<CancellationToken, Task<string[]>>? supplier =
            message.BookmarksSupplierRegistered ? SupplyFromManager : null;

        Func<string[], CancellationToken, Task>? consumer =
            message.BookmarksConsumerRegistered ? ConsumeFromManager : null;

        return GraphDatabase.BookmarkManagerFactory.NewBookmarkManager(
            new BookmarkManagerConfig(message.InitialBookmarks, supplier, consumer));
    }

    private async Task<string[]> SupplyBookmarksAsync(string managerId)
    {
        return await _roundTrip.SendExpectingAsync<string[]>(new BookmarksSupplierRequest(managerId));
    }

    private async Task ConsumeBookmarksAsync(string managerId, string[] bookmarks)
    {
        await _roundTrip.SendExpectingAsync<bool>(new BookmarksConsumerRequest(managerId, bookmarks));
    }
}
