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

using System.Reflection;
using System.Runtime.ExceptionServices;
using Neo4j.Driver.TestKitBackend.ObjectStorage;

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
        nameof(NewSessionRequest.BookmarkManagerId),
        nameof(NewSessionRequest.Driver)
    ];

    private readonly IObjectStore _objectStore;

    public NewSessionConfigMapper(IObjectStore objectStore)
    {
        _objectStore = objectStore;
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
            builder.WithBookmarkManager(_objectStore.Get<IBookmarkManager>(bookmarkManagerId).Object);
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

    private static void ApplyRemainingProperties(NewSessionRequest request, ISessionConfigBuilder builder)
    {
        foreach (var property in request.GetType().GetProperties())
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

            var methodName = "With" + property.Name;
            var method = typeof(ISessionConfigBuilder).GetMethod(methodName) ??
                throw new InvalidOperationException(
                    $"No {methodName} method found on {nameof(ISessionConfigBuilder)} for {property.Name}.");

            try
            {
                method.Invoke(builder, [value]);
            }
            catch (TargetInvocationException e) when (e.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(e.InnerException).Throw();
            }
        }
    }
}
