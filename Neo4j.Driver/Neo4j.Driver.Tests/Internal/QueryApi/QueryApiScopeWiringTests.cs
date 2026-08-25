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
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.DependencyInjection;
using Neo4j.Driver.Internal.QueryApi;
using Neo4j.Driver.Tests.Internal.Core;
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

    [Fact]
    public async Task SecondTransactionInSession_StillRollsBackOverNetwork_AfterFirstTransactionFailedServerSide()
    {
        var (session, requestBuilder) = BuildWiredSessionScopeForSequentialTransactions();

        var tx1 = await session.BeginTransactionAsync(AccessMode.Write, null!);
        var runTx1 = () => tx1.RunAsync("MATCH (n) RETURN n");
        await runTx1.Should().ThrowAsync<ClientException>();
        await tx1.RollbackAsync();

        var tx2 = await session.BeginTransactionAsync(AccessMode.Write, null!);
        await tx2.RollbackAsync();

        requestBuilder.Verify(
            x => x.DeleteAsync("query/v2/tx/tx-1", It.IsAny<CancellationToken>()),
            Times.Never);

        requestBuilder.Verify(
            x => x.DeleteAsync("query/v2/tx/tx-2", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ClusterAffinityHeader_IsApplied_ForRequestsInsideATransaction()
    {
        var (sessionScope, _) = BuildScopeWithRealRequestBuilder();
        var transactionScope = CreateTransactionScope(sessionScope);

        transactionScope.Resolve<IQueryApiTransactionContextTracker>()
            .Set(new QueryApiTransactionContext("tx-1", "affinity-abc"));

        using var request = await transactionScope
            .Resolve<IQueryApiRequestBuilder>()
            .DeleteAsync("query/v2/tx/tx-1", TestContext.Current.CancellationToken);

        request.Headers.GetValues("neo4j-cluster-affinity").Should().BeEquivalentTo(["affinity-abc"]);
    }

    [Fact]
    public async Task RequestBuilder_IsUsableOutsideATransaction_ForAutoCommit()
    {
        var (sessionScope, _) = BuildScopeWithRealRequestBuilder();

        using var request = await sessionScope
            .Resolve<IQueryApiRequestBuilder>()
            .DeleteAsync("query/v2/tx/tx-1", TestContext.Current.CancellationToken);

        request.Headers.Contains("neo4j-cluster-affinity").Should().BeFalse();
        sessionScope.Resolve<IEnumerable<IHttpRequestEnricher>>().Should().BeEmpty();
    }

    [Fact]
    public async Task DisposingTheTransactionScope_ReleasesTheTransactionLoggingContext()
    {
        var logContextTracker = new LoggingContextTracker();
        var driverScope = new ScopedContainer();
        driverScope.RegisterInstance<ILoggingContextTracker>(logContextTracker);

        var transactionScope = CreateTransactionScope(driverScope);
        transactionScope.Resolve<IQueryApiTransactionContextTracker>()
            .Set(new QueryApiTransactionContext("tx-1", null));

        logContextTracker.Contexts.Should().ContainSingle(c => c.Key == "transaction");

        await transactionScope.DisposeAsync();

        logContextTracker.Contexts.Should().BeEmpty();
    }

    private static (IResolutionScope sessionScope, ScopedContainer driverScope) BuildScopeWithRealRequestBuilder()
    {
        var urlBuilder = new Mock<IQueryApiUrlBuilder>();
        urlBuilder.Setup(x => x.Build(It.IsAny<string>())).Returns(new Uri("http://localhost:7474/"));

        var driverScope = new ScopedContainer();
        driverScope.RegisterInstance<ILogger>(new TestLogger(typeof(QueryApiRequestBuilder)));
        driverScope.RegisterInstance(urlBuilder.Object);
        driverScope.RegisterInstance(Mock.Of<IQueryApiJsonSerializer>());
        driverScope.RegisterInstance(Mock.Of<IQueryApiRequestHeaderWriter>());
        driverScope.RegisterInstance<ILoggingContextTracker>(new LoggingContextTracker());
        driverScope.RegisterType<IQueryApiRequestBuilder, QueryApiRequestBuilder>();

        var sessionScope = driverScope.CreateChildScope(r => r
            .RegisterInstance(Mock.Of<ISessionContext>(c => c.Database == "neo4j")));

        return (sessionScope, driverScope);
    }

    private static IResolutionScope CreateTransactionScope(IResolutionScope sessionScope)
    {
        return sessionScope.CreateChildScope(r => r
            .RegisterType<IQueryApiTransactionContextTracker, QueryApiTransactionContextTracker>(singleton: true)
            .RegisterType<IHttpRequestEnricher, QueryApiClusterAffinityEnricher>());
    }

    private static (IInternalAsyncSession session, Mock<IQueryApiRequestBuilder> requestBuilder)
        BuildWiredSessionScopeForSequentialTransactions()
    {
        var beginRequestTx1 = new HttpRequestMessage();
        var beginRequestTx2 = new HttpRequestMessage();
        var runRequestTx1 = new HttpRequestMessage();
        var deleteRequestTx2 = new HttpRequestMessage();
        var headers = new HttpResponseMessage().Headers;

        var beginCallCount = 0;
        var requestBuilder = new Mock<IQueryApiRequestBuilder>();
        requestBuilder
            .Setup(x => x.PostAsync("query/v2/tx", It.IsAny<IQueryApiRequestBody>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => ++beginCallCount == 1 ? beginRequestTx1 : beginRequestTx2);
        requestBuilder
            .Setup(x => x.PostAsync("query/v2/tx/tx-1", It.IsAny<IQueryApiRequestBody>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(runRequestTx1);
        requestBuilder
            .Setup(x => x.DeleteAsync("query/v2/tx/tx-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(deleteRequestTx2);

        var client = new Mock<IQueryApiClient>();
        client
            .Setup(x => x.ExecuteAsync<TransactionBeginner.ResponseBody>(beginRequestTx1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryApiResult<TransactionBeginner.ResponseBody>(
                new TransactionBeginner.ResponseBody { Transaction = new TransactionBeginner.TransactionInfo("tx-1") },
                headers));
        client
            .Setup(x => x.ExecuteAsync<TransactionBeginner.ResponseBody>(beginRequestTx2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryApiResult<TransactionBeginner.ResponseBody>(
                new TransactionBeginner.ResponseBody { Transaction = new TransactionBeginner.TransactionInfo("tx-2") },
                headers));
        client
            .Setup(x => x.ExecuteAsync<QueryApiResultBody>(runRequestTx1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ClientException("Neo.ClientError.Statement.SyntaxError", "bad query"));

        var httpTransport = new Mock<IQueryApiHttpTransport>();
        httpTransport
            .Setup(x => x.SendAsync(deleteRequestTx2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage());

        var parentScope = new ScopedContainer();
        parentScope.RegisterInstance<ILogger>(new TestLogger(typeof(QueryApiSession)));
        parentScope.RegisterInstance<IAsyncRetryLogic>(new SimpleRetryLogic(fn => fn()));
        parentScope.RegisterInstance(requestBuilder.Object);
        parentScope.RegisterInstance(client.Object);
        parentScope.RegisterInstance(httpTransport.Object);
        parentScope.RegisterInstance(Mock.Of<IClusterAffinityExtractor>());
        parentScope.RegisterInstance(Mock.Of<ISessionContext>());
        parentScope.RegisterInstance(Mock.Of<IQueryApiResultCursorBuilder>());
        parentScope.RegisterInstance(Mock.Of<ITransactionCommitter>());
        parentScope.RegisterInstance(Mock.Of<IAutoCommitRunner>());
        parentScope.RegisterType<ITransactionBeginner, TransactionBeginner>();
        parentScope.RegisterType<ITransactionRunner, TransactionRunner>();
        parentScope.RegisterType<ITransactionRollback, TransactionRollbacker>();
        parentScope.RegisterType<IScopedTransaction, QueryApiTransaction>();
        parentScope.RegisterType<IQueryApiTransactionFactory, QueryApiTransactionFactory>();
        parentScope.RegisterType<IInternalAsyncSession, QueryApiSession>();

        var sessionScope = parentScope.CreateChildScope(r => r
            .RegisterInstance(SessionConfig.Builder.Build())
            .RegisterType<IQueryApiTransactionContextTracker, QueryApiTransactionContextTracker>(singleton: true)
            .RegisterType<IBookmarkTracker, BookmarkTracker>(singleton: true)
            .RegisterType<ILoggingContextTracker, LoggingContextTracker>(singleton: true));

        return (sessionScope.Resolve<IInternalAsyncSession>(), requestBuilder);
    }

    private static (IResolutionScope sessionScope, BookmarkTracker tracker) BuildWiredSessionScope()
    {
        var holder = new QueryApiTransactionContextTracker(new LoggingContextTracker());

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
        parentScope.RegisterType<IScopedTransaction, QueryApiTransaction>();
        parentScope.RegisterType<IQueryApiTransactionFactory, QueryApiTransactionFactory>();
        parentScope.RegisterType<IInternalAsyncSession, QueryApiSession>();

        var sessionScope = parentScope.CreateChildScope(r => r
            .RegisterInstance(SessionConfig.Builder.Build())
            .RegisterInstance<IQueryApiTransactionContextTracker>(holder)
            .RegisterType<IBookmarkTracker, BookmarkTracker>(singleton: true)
            .RegisterType<ILoggingContextTracker, LoggingContextTracker>(singleton: true));

        trackerRef = (BookmarkTracker)sessionScope.Resolve<IBookmarkTracker>();

        return (sessionScope, trackerRef);
    }
}
