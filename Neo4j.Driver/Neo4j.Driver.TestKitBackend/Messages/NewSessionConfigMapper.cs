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

using Neo4j.Driver.TestKitBackend.ObjectRegistry;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal interface INewSessionConfigMapper
{
    void Apply(NewSessionRequest request, ISessionConfigBuilder builder);
}

internal class NewSessionConfigMapper : INewSessionConfigMapper
{
    // Properties applied by the special cases below - excluded from the generic tier so they're
    // never also (mis)matched by name against ISessionConfigBuilder there.
    private static readonly HashSet<string> HandledExplicitly =
    [
        nameof(NewSessionRequest.AccessMode),
        nameof(NewSessionRequest.Bookmarks),
        nameof(NewSessionRequest.AuthorizationToken),
        nameof(NewSessionRequest.NotificationsMinSeverity),
        nameof(NewSessionRequest.NotificationsDisabledCategories),
        nameof(NewSessionRequest.BookmarkManagerId)
    ];

    private readonly IRegistry _registry;

    public NewSessionConfigMapper(IRegistry registry)
    {
        _registry = registry;
    }

    public void Apply(NewSessionRequest request, ISessionConfigBuilder builder)
    {
        ApplyAccessMode(request, builder);
        ApplyBookmarks(request, builder);
        ApplyAuthorizationToken(request, builder);
        ApplyNotifications(request, builder);
        ApplyBookmarkManager(request, builder);
        ApplyRemainingProperties(request, builder);
    }

    private void ApplyBookmarkManager(NewSessionRequest request, ISessionConfigBuilder builder)
    {
        if (request.BookmarkManagerId is { } bookmarkManagerId)
        {
            builder.WithBookmarkManager(_registry.Get<IBookmarkManager>(bookmarkManagerId).Object);
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
            builder.WithAuthToken(token.Value.ToAuthToken());
        }
    }

    private static void ApplyNotifications(NewSessionRequest request, ISessionConfigBuilder builder)
    {
        if (request.NotificationsMinSeverity is null && request.NotificationsDisabledCategories is null)
        {
            return;
        }

        if (request.NotificationsMinSeverity == "OFF")
        {
            builder.WithNotificationsDisabled();
            return;
        }

        var severity = request.NotificationsMinSeverity is { } minSeverity
            ? Enum.Parse<Severity>(minSeverity, true)
            : (Severity?)null;

        var categories = request.NotificationsDisabledCategories
            ?.Select(c => Enum.Parse<Category>(c, true))
            .ToArray();

        builder.WithNotifications(severity, categories);
    }

    // The remaining fields are a direct name match (With{PropertyName}). If ISessionConfigBuilder
    // has no matching method, the field is silently skipped - it's either not session config
    // (e.g. Driver) or not wired up yet (e.g. bookmarkManagerId).
    private static void ApplyRemainingProperties(NewSessionRequest request, ISessionConfigBuilder builder)
    {
        foreach (var property in typeof(NewSessionRequest).GetProperties())
        {
            if (HandledExplicitly.Contains(property.Name))
            {
                continue;
            }

            var value = property.GetValue(request);
            if (value is null)
            {
                continue;
            }

            var method = typeof(ISessionConfigBuilder).GetMethod("With" + property.Name);
            method?.Invoke(builder, [value]);
        }
    }
}
