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

namespace Neo4j.Driver.TestKitBackend.Messages;

internal interface INewSessionConfigMapper
{
    void Apply(NewSessionRequest request, ISessionConfigBuilder builder);
}

internal class NewSessionConfigMapper : INewSessionConfigMapper
{
    private static readonly HashSet<string> HandledExplicitly =
    [
        nameof(NewSessionRequest.AccessMode),
        nameof(NewSessionRequest.Bookmarks),
        nameof(NewSessionRequest.AuthorizationToken),
        nameof(NewSessionRequest.NotificationsMinSeverity),
        nameof(NewSessionRequest.NotificationsDisabledCategories),
        nameof(NewSessionRequest.BookmarkManager),
        nameof(NewSessionRequest.Driver)
    ];

    private readonly INotificationsMapper _notificationsMapper;
    private readonly IRemainingPropertiesMapper _remainingPropertiesMapper;

    public NewSessionConfigMapper(
        INotificationsMapper notificationsMapper,
        IRemainingPropertiesMapper remainingPropertiesMapper)
    {
        _notificationsMapper = notificationsMapper;
        _remainingPropertiesMapper = remainingPropertiesMapper;
    }

    public void Apply(NewSessionRequest request, ISessionConfigBuilder builder)
    {
        ApplyAccessMode(request, builder);
        ApplyBookmarks(request, builder);
        ApplyAuthorizationToken(request, builder);
        ApplyNotifications(request, builder);
        ApplyBookmarkManager(request, builder);
        _remainingPropertiesMapper.Apply(request, builder, HandledExplicitly);
    }

    private static void ApplyBookmarkManager(NewSessionRequest request, ISessionConfigBuilder builder)
    {
        if (request.BookmarkManager is { } bookmarkManager)
        {
            builder.WithBookmarkManager(bookmarkManager);
        }
    }

    private static void ApplyAccessMode(NewSessionRequest request, ISessionConfigBuilder builder)
    {
        builder.WithDefaultAccessMode(request.AccessMode == "r" ? AccessMode.Read : AccessMode.Write);
    }

    private static void ApplyBookmarks(NewSessionRequest request, ISessionConfigBuilder builder)
    {
        if (request.Bookmarks is { } bookmarks)
        {
            builder.WithBookmarks(Bookmarks.From(bookmarks));
        }
    }

    private static void ApplyAuthorizationToken(NewSessionRequest request, ISessionConfigBuilder builder)
    {
        if (request.AuthorizationToken is { } token)
        {
            builder.WithAuthToken(token.ToAuthToken());
        }
    }

    private void ApplyNotifications(NewSessionRequest request, ISessionConfigBuilder builder)
    {
        _notificationsMapper.Apply(
            request.NotificationsMinSeverity,
            request.NotificationsDisabledCategories,
            () => builder.WithNotificationsDisabled(),
            (severity, categories) => builder.WithNotifications(severity, categories));
    }

}
