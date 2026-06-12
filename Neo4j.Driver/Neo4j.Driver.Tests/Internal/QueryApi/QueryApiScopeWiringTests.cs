// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#nullable enable

using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.DependencyInjection;
using Neo4j.Driver.Internal.QueryApi;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

public class QueryApiScopeWiringTests
{
    private static readonly string[] CommittedBookmarks = ["neo4j:bookmark:v1:tx42"];

    [Fact]
    public async Task CommittedBookmarks_AreVisibleToTracker_AfterCommit()
    {
        var (sessionScope, tracker) = BuildWiredSessionScope();
        var session = sessionScope.Resolve<IInternalAsyncSession>();

        var tx = await session.BeginTransactionAsync(AccessMode.Write, null!);
        await tx.CommitAsync();

        tracker.CurrentBookmarks.Values.Should().BeEquivalentTo(CommittedBookmarks);
    }

    private static (IResolutionScope sessionScope, BookmarkTracker tracker) BuildWiredSessionScope()
    {
        var holder = new QueryApiTransactionContextTracker();

        var starterMock = new Mock<ITransactionBeginner>();
        starterMock
            .Setup(s => s.BeginAsync(It.IsAny<CancellationToken>()))
            .Callback(() => holder.Set(new QueryApiTransactionContext("tx-1", null)))
            .Returns(Task.CompletedTask);

        // Capture tracker by reference so the callback can update bookmarks after the scope is built.
        BookmarkTracker? trackerRef = null;
        var committerMock = new Mock<ITransactionCommitter>();
        committerMock
            .Setup(c => c.CommitAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(_ =>
            {
                // ReSharper disable once AccessToModifiedClosure
                trackerRef?.UpdateBookmarks(CommittedBookmarks);
                return Task.CompletedTask;
            });

        var parentScope = new ScopedContainer();
        parentScope.RegisterInstance<ILogger>(new TestLogger(typeof(QueryApiSession)));
        parentScope.RegisterInstance<IAsyncRetryLogic>(new SimpleRetryLogic(fn => fn()));
        parentScope.RegisterInstance(starterMock.Object);
        parentScope.RegisterInstance(committerMock.Object);
        parentScope.RegisterInstance(Mock.Of<ITransactionRollback>());
        parentScope.RegisterInstance(Mock.Of<ITransactionRunner>());
        parentScope.RegisterInstance(Mock.Of<IAutoCommitRunner>());
        parentScope.RegisterInstance(holder);
        parentScope.RegisterType<IInternalAsyncTransaction, QueryApiTransaction>();
        parentScope.RegisterType<IQueryApiTransactionFactory, QueryApiTransactionFactory>();
        parentScope.RegisterType<IInternalAsyncSession, QueryApiSession>();

        var sessionScope = parentScope.CreateChildScope(r => r
            .RegisterInstance(SessionConfig.Builder.Build())
            .RegisterType<IBookmarkTracker, BookmarkTracker>(singleton: true)
            .RegisterType<ILoggingContextTracker, LoggingContextTracker>(singleton: true));

        trackerRef = (BookmarkTracker)sessionScope.Resolve<IBookmarkTracker>();

        return (sessionScope, trackerRef);
    }
}
