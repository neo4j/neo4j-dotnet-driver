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

internal interface ISessionConfigBuilder
{
    ISessionConfigBuilder WithDatabase(string database);
    ISessionConfigBuilder WithDefaultAccessMode(AccessMode defaultAccessMode);
    ISessionConfigBuilder WithAuthToken(IAuthToken authToken);
    ISessionConfigBuilder WithBookmarks(params Bookmarks[] bookmarks);
    ISessionConfigBuilder WithFetchSize(long size);
    ISessionConfigBuilder WithImpersonatedUser(string impersonatedUser);
    ISessionConfigBuilder WithNotificationsDisabled();
    ISessionConfigBuilder WithNotifications(Severity? minimumSeverity, Category[]? disabledCategories);
    ISessionConfigBuilder WithDisableAutoCommitRetries(bool disable);
    ISessionConfigBuilder WithBookmarkManager(IBookmarkManager bookmarkManager);
}

internal class SessionConfigBuilderAdapter : ISessionConfigBuilder
{
    private readonly SessionConfigBuilder _builder;

    public SessionConfigBuilderAdapter(SessionConfigBuilder builder)
    {
        _builder = builder;
    }

    public ISessionConfigBuilder WithDatabase(string database)
    {
        _builder.WithDatabase(database);
        return this;
    }

    public ISessionConfigBuilder WithDefaultAccessMode(AccessMode defaultAccessMode)
    {
        _builder.WithDefaultAccessMode(defaultAccessMode);
        return this;
    }

    public ISessionConfigBuilder WithAuthToken(IAuthToken authToken)
    {
        _builder.WithAuthToken(authToken);
        return this;
    }

    public ISessionConfigBuilder WithBookmarks(params Bookmarks[] bookmarks)
    {
        _builder.WithBookmarks(bookmarks);
        return this;
    }

    public ISessionConfigBuilder WithFetchSize(long size)
    {
        _builder.WithFetchSize(size);
        return this;
    }

    public ISessionConfigBuilder WithImpersonatedUser(string impersonatedUser)
    {
        _builder.WithImpersonatedUser(impersonatedUser);
        return this;
    }

    public ISessionConfigBuilder WithNotificationsDisabled()
    {
        _builder.WithNotificationsDisabled();
        return this;
    }

    public ISessionConfigBuilder WithNotifications(Severity? minimumSeverity, Category[]? disabledCategories)
    {
        _builder.WithNotifications(minimumSeverity, disabledCategories);
        return this;
    }

    public ISessionConfigBuilder WithDisableAutoCommitRetries(bool disable)
    {
        _builder.WithDisableAutoCommitRetries(disable);
        return this;
    }

    public ISessionConfigBuilder WithBookmarkManager(IBookmarkManager bookmarkManager)
    {
        _builder.WithBookmarkManager(bookmarkManager);
        return this;
    }
}
