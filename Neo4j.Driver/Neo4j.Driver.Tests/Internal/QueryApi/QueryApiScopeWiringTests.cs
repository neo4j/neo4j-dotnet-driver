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

using System;
using System.Collections.Generic;
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
    /// <summary>
    /// Verifies that bookmarks committed in a transaction are tracked by the session-scoped
    /// <see cref="IBookmarkTracker"/> and are visible to subsequent operations. This exercises the DI
    /// scope wiring: tracker is shared between commit handler (which updates it) and begin handler
    /// (which reads from it), both receiving the same singleton via child-scope inheritance.
    /// </summary>
    [Fact]
    public async Task CommittedBookmarks_AreVisibleToTracker_AfterCommit()
    {
        var committedBookmarks = new[] { "neo4j:bookmark:v1:tx42" };
        var holder = new QueryApiTransactionContextHolder();

        var starterMock = new Mock<ITransactionBeginner>();
        starterMock
            .Setup(s => s.BeginAsync(It.IsAny<CancellationToken>()))
            .Callback(() => holder.Set(new QueryApiTransactionContext("tx-1", null)))
            .Returns(Task.CompletedTask);

        // The committer simulates what TransactionCommitter does: update the tracker.
        // We wire this via a callback so it uses the session-scoped tracker.
        BookmarkTracker? trackerRef = null;
        var committerMock = new Mock<ITransactionCommitter>();
        committerMock
            .Setup(c => c.CommitAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(_ =>
            {
                trackerRef?.UpdateBookmarks(committedBookmarks);
                return Task.CompletedTask;
            });

        var parentScope = new ScopedContainer();
        parentScope.RegisterInstance<ILogger>(new TestLogger(typeof(QueryApiSession)));
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

        var session = sessionScope.Resolve<IInternalAsyncSession>();

        var tx = await session.BeginTransactionAsync(AccessMode.Write, null!);
        await tx.CommitAsync();

        // After commit, the tracker should have the bookmarks that TransactionCommitter
        // would have written. In production code, this is done via IBookmarkTracker injection.
        trackerRef.CurrentBookmarks.Values.Should().BeEquivalentTo(committedBookmarks);

        // A second begin can start — verifying the factory+holder mechanism works twice
        await session.BeginTransactionAsync(AccessMode.Write, null!);
        starterMock.Verify(s => s.BeginAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task SessionClose_DisposesSessionScopeAndItsChildren()
    {
        var disposalOrder = new List<string>();

        var holder = new QueryApiTransactionContextHolder();
        var starterMock = new Mock<ITransactionBeginner>();
        starterMock
            .Setup(s => s.BeginAsync(It.IsAny<CancellationToken>()))
            .Callback(() => holder.Set(new QueryApiTransactionContext("tx-1", null)))
            .Returns(Task.CompletedTask);

        var rollbackMock = new Mock<ITransactionRollback>();
        rollbackMock
            .Setup(r => r.RollbackAsync(It.IsAny<CancellationToken>()))
            .Callback(() => disposalOrder.Add("rollback"))
            .Returns(Task.CompletedTask);

        var parentScope = new ScopedContainer();
        parentScope.RegisterInstance<ILogger>(new TestLogger(typeof(QueryApiSession)));
        parentScope.RegisterInstance(starterMock.Object);
        parentScope.RegisterInstance(Mock.Of<ITransactionCommitter>());
        parentScope.RegisterInstance(rollbackMock.Object);
        parentScope.RegisterInstance(Mock.Of<ITransactionRunner>());
        parentScope.RegisterInstance(Mock.Of<IAutoCommitRunner>());
        parentScope.RegisterInstance(holder);
        parentScope.RegisterType<IInternalAsyncTransaction, QueryApiTransaction>();
        parentScope.RegisterType<IQueryApiTransactionFactory, QueryApiTransactionFactory>();
        parentScope.RegisterType<IInternalAsyncSession, QueryApiSession>();

        var sessionScope = (ScopedContainer)parentScope.CreateChildScope(r => r
            .RegisterInstance(SessionConfig.Builder.Build())
            .RegisterType<IBookmarkTracker, BookmarkTracker>(singleton: true)
            .RegisterType<ILoggingContextTracker, LoggingContextTracker>(singleton: true));

        var session = sessionScope.Resolve<IInternalAsyncSession>();

        // Subscribe last — the scope's wiring will subscribe first, so "rollback" appears
        // before this handler fires.
        session.Disposed += (_, _) =>
        {
            disposalOrder.Add("session-disposed");
            return Task.CompletedTask;
        };

        // Begin a transaction so a child scope (tx scope) exists inside the session scope.
        await session.BeginTransactionAsync(AccessMode.Write, null!);

        // Close the session. Expectation: the session scope disposes (rolling back the open
        // transaction), then the test's Disposed handler fires last.
        await session.CloseAsync();

        // Scope should be disposed — any further use throws.
        var act = () => sessionScope.CreateChildScope(_ => {});
        act.Should().Throw<ObjectDisposedException>();

        // Rollback must precede session-disposed: the tx is a child scope, disposed before
        // the session's own Disposed event reaches the test subscriber.
        disposalOrder.Should().Equal("rollback", "session-disposed");
    }
}
