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
using Neo4j.Driver.TestKitBackend.ObjectStorage;
using Neo4j.Driver.TestKitBackend.Serialization;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal record NewSessionRequest : IProtocolMessage
{
    [StoredObject]
    public required IDriver Driver { get; init; }
    public required string AccessMode { get; init; }
    public string[]? Bookmarks { get; init; }
    public string? Database { get; init; }
    public long? FetchSize { get; init; }
    public string? ImpersonatedUser { get; init; }
    public string? NotificationsMinSeverity { get; init; }
    public string[]? NotificationsDisabledCategories { get; init; }
    public bool? DisableAutoCommitRetries { get; init; }
    [StoredObject]
    public IBookmarkManager? BookmarkManager { get; init; }
    public AuthorizationToken? AuthorizationToken { get; init; }
}

internal record SessionResponse(string Id) : IProtocolMessage;

internal class NewSessionHandler : MessageHandler<NewSessionRequest>
{
    private readonly IObjectStore _objectStore;
    private readonly INewSessionConfigMapper _configMapper;
    private readonly IResponseWriter _responseWriter;
    private readonly ILogger _logger;

    public NewSessionHandler(
        IObjectStore objectStore,
        INewSessionConfigMapper configMapper,
        IResponseWriter responseWriter,
        ILogger logger)
    {
        _objectStore = objectStore;
        _configMapper = configMapper;
        _responseWriter = responseWriter;
        _logger = logger;
    }

    public override async Task ProcessAsync(NewSessionRequest message)
    {
        var session = message.Driver.AsyncSession(
            builder => _configMapper.Apply(message, new SessionConfigBuilderAdapter(builder)));
        var id = _objectStore.Store(session);
        _logger.LogDebug("Created session with id '{Id}'", id);
        await _responseWriter.WriteAsync(new SessionResponse(id));
    }
}
