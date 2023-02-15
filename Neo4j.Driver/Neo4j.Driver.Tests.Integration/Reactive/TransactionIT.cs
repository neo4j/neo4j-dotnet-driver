// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using Neo4j.Driver.IntegrationTests.Internals;
using Neo4j.Driver.Reactive;
using Xunit.Abstractions;
using static Neo4j.Driver.IntegrationTests.Internals.VersionComparison;
using static Neo4j.Driver.Reactive.Utils;
using static Neo4j.Driver.Tests.Assertions;

namespace Neo4j.Driver.IntegrationTests.Reactive;

public sealed class TransactionIT : AbstractRxIT
{
    private readonly IRxSession _session;

    public TransactionIT(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
        : base(output, fixture)
    {
        // clean database after each test run
        using (var tmpSession = Server.Driver.Session())
        {
            tmpSession.Run("MATCH (n) DETACH DELETE n").Consume();
        }

        _session = NewSession();
    }

    [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
    public void ShouldCommitEmptyTx()
    {
        var bookmarkBefore = _session.LastBookmarks;

        _session.BeginTransaction()
            .SelectMany(tx => tx.Commit<int>())
            .WaitForCompletion()
            .AssertEqual(OnCompleted<int>(0));

        var bookmarkAfter = _session.LastBookmarks;

        bookmarkBefore.Should().BeNull();
        bookmarkAfter.Should().NotBe(bookmarkBefore);
    }

    [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
    public void ShouldRollbackEmptyTx()
    {
        var bookmarkBefore = _session.LastBookmarks;

        _session.BeginTransaction()
            .SelectMany(tx => tx.Rollback<int>())
            .WaitForCompletion()
            .AssertEqual(OnCompleted<int>(0));

        var bookmarkAfter = _session.LastBookmarks;

        bookmarkBefore.Should().BeNull();
        bookmarkAfter.Should().Be(bookmarkBefore);
    }

    [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
    public void ShouldRunQueryAndCommit()
    {
        _session.BeginTransaction()
            .SelectMany(
                txc =>
                    txc.Run("CREATE (n:Node {id: 42}) RETURN n")
                        .Records()
                        .Select(r => r["n"].As<INode>()["id"].As<int>())
                        .Concat(txc.Commit<int>())
                        .Catch(txc.Rollback<int>()))
            .WaitForCompletion()
            .AssertEqual(
                OnNext(0, 42),
                OnCompleted<int>(0));

        CountNodes(42).Should().Be(1);
    }

    [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
    public async Task ShouldRunQueryAndRollback()
    {
        var txc = await _session.BeginTransaction().SingleAsync();

        VerifyCanCreateNode(txc, 4242);
        VerifyCanRollback(txc);

        CountNodes(4242).Should().Be(0);
    }

    [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
    public Task ShouldRunMultipleQuerysAndCommit()
    {
        return VerifyRunMultipleQuerys(true);
    }

    [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
    public Task ShouldRunMultipleQuerysAndRollback()
    {
        return VerifyRunMultipleQuerys(false);
    }

    private async Task VerifyRunMultipleQuerys(bool commit)
    {
        var txc = await _session.BeginTransaction().SingleAsync();

        var result1 = txc.Run("CREATE (n:Node {id : 1})");
        await result1.Records().SingleOrDefaultAsync();

        var result2 = txc.Run("CREATE (n:Node {id : 2})");
        await result2.Records().SingleOrDefaultAsync();

        var result3 = txc.Run("CREATE (n:Node {id : 1})");
        await result3.Records().SingleOrDefaultAsync();

        VerifyCanCommitOrRollback(txc, commit);

        VerifyCommittedOrRollbacked(commit);
    }

    [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
    public Task ShouldRunMultipleQuerysWithoutWaitingAndCommit()
    {
        return VerifyRunMultipleQuerysWithoutWaiting(true);
    }

    [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
    public Task ShouldRunMultipleQuerysWithoutWaitingAndRollback()
    {
        return VerifyRunMultipleQuerysWithoutWaiting(false);
    }

    private async Task VerifyRunMultipleQuerysWithoutWaiting(bool commit)
    {
        var txc = await _session.BeginTransaction().SingleAsync();

        var result1 = txc.Run("CREATE (n:Node {id : 1})");
        var result2 = txc.Run("CREATE (n:Node {id : 2})");
        var result3 = txc.Run("CREATE (n:Node {id : 1})");

        await result1.Records().Concat(result2.Records()).Concat(result3.Records()).SingleOrDefaultAsync();

        VerifyCanCommitOrRollback(txc, commit);

        VerifyCommittedOrRollbacked(commit);
    }

    [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
    public Task ShouldRunMultipleQuerysWithoutStreamingAndCommit()
    {
        return VerifyRunMultipleQuerysWithoutStreaming(true);
    }

    [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
    public Task ShouldRunMultipleQuerysWithoutStreamingAndRollback()
    {
        return VerifyRunMultipleQuerysWithoutStreaming(false);
    }

    private async Task VerifyRunMultipleQuerysWithoutStreaming(bool commit)
    {
        var txc = await _session.BeginTransaction().SingleAsync();

        var result1 = txc.Run("CREATE (n:Node {id : 1})");
        var result2 = txc.Run("CREATE (n:Node {id : 2})");
        var result3 = txc.Run("CREATE (n:Node {id : 1})");

        await result1.Keys().Concat(result2.Keys()).Concat(result3.Keys());

        VerifyCanCommitOrRollback(txc, commit);

        VerifyCommittedOrRollbacked(commit);
    }

    [RequireServerFact]
    public async Task ShouldFailToCommitAfterAFailedQuery()
    {
        var txc = await _session.BeginTransaction().SingleAsync();

        VerifyFailsWithWrongQuery(txc);

        txc.Commit<int>()
            .WaitForCompletion()
            .AssertEqual(OnError<int>(0, MatchesException<ClientException>()));
    }

    [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
    public async Task ShouldSucceedToRollbackAfterAFailedQuery()
    {
        var txc = await _session.BeginTransaction().SingleAsync();

        VerifyFailsWithWrongQuery(txc);

        txc.Rollback<int>()
            .WaitForCompletion()
            .AssertEqual(OnCompleted<int>(0));
    }

    [RequireServerFact]
    public async Task ShouldFailToCommitAfterSuccessfulAndFailedQuerys()
    {
        var txc = await _session.BeginTransaction().SingleAsync();

        VerifyCanCreateNode(txc, 5);
        VerifyCanReturnOne(txc);
        VerifyFailsWithWrongQuery(txc);

        txc.Commit<int>()
            .WaitForCompletion()
            .AssertEqual(OnError<int>(0, MatchesException<ClientException>()));
    }

    [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
    public async Task ShouldSucceedToRollbackAfterSuccessfulAndFailedQuerys()
    {
        var txc = await _session.BeginTransaction().SingleAsync();

        VerifyCanCreateNode(txc, 5);
        VerifyCanReturnOne(txc);
        VerifyFailsWithWrongQuery(txc);

        txc.Rollback<int>()
            .WaitForCompletion()
            .AssertEqual(OnCompleted<int>(0));
    }

    [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
    public async Task ShouldFailToRunAnotherQueryAfterAFailedOne()
    {
        var txc = await _session.BeginTransaction().SingleAsync();

        VerifyFailsWithWrongQuery(txc);

        txc.Run("CREATE ()")
            .Records()
            .WaitForCompletion()
            .AssertEqual(
                OnError<IRecord>(
                    0,
                    Matches<Exception>(
                        exc =>
                            exc.Message.Should().Contain("Cannot run query in this transaction"))));

        VerifyCanRollback(txc);
    }

    [RequireServerFact]
    public void ShouldFailToBeginTxcWithInvalidBookmark()
    {
        Server.Driver
            .RxSession(o => o.WithDefaultAccessMode(AccessMode.Read).WithBookmarks(Bookmarks.From("InvalidBookmark")))
            .BeginTransaction()
            .WaitForCompletion()
            .AssertEqual(
                OnError<IRxTransaction>(
                    0,
                    Matches<Exception>(exc => exc.Message.Should().Contain("InvalidBookmark"))));
    }

    [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
    public async Task ShouldNotAllowCommitAfterCommit()
    {
        var txc = await _session.BeginTransaction().SingleAsync();

        VerifyCanCreateNode(txc, 6);
        VerifyCanCommit(txc);

        txc.Commit<int>()
            .WaitForCompletion()
            .AssertEqual(
                OnError<int>(
                    0,
                    Matches<Exception>(
                        exc =>
                            exc.Message.Should()
                                .Be("Cannot commit this transaction, because it has already been committed."))));
    }

    [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
    public async Task ShouldNotAllowRollbackAfterRollback()
    {
        var txc = await _session.BeginTransaction().SingleAsync();

        VerifyCanCreateNode(txc, 6);
        VerifyCanRollback(txc);

        txc.Rollback<int>()
            .WaitForCompletion()
            .AssertEqual(
                OnError<int>(
                    0,
                    Matches<Exception>(
                        exc =>
                            exc.Message.Should()
                                .Be("Cannot rollback this transaction, because it has already been rolled back."))));
    }

    [RequireServerFact]
    public async Task ShouldFailToCommitAfterRollback()
    {
        var txc = await _session.BeginTransaction().SingleAsync();

        VerifyCanCreateNode(txc, 6);
        VerifyCanRollback(txc);

        txc.Commit<int>()
            .WaitForCompletion()
            .AssertEqual(
                OnError<int>(
                    0,
                    Matches<Exception>(
                        exc =>
                            exc.Message.Should()
                                .Be("Cannot commit this transaction, because it has already been rolled back."))));
    }

    [RequireServerFact]
    public async Task ShouldFailToRollbackAfterCommit()
    {
        var txc = await _session.BeginTransaction().SingleAsync();

        VerifyCanCreateNode(txc, 6);
        VerifyCanCommit(txc);

        txc.Rollback<string>()
            .WaitForCompletion()
            .AssertEqual(
                OnError<string>(
                    0,
                    Matches<Exception>(
                        exc =>
                            exc.Message.Should()
                                .Be("Cannot rollback this transaction, because it has already been committed."))));
    }

    [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
    public Task ShouldFailToRunQueryAfterCompletedTxcAndCommit()
    {
        return VerifyFailToRunQueryAfterCompletedTxc(true);
    }

    [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
    public Task ShouldFailToRunQueryAfterCompletedTxcAndRollback()
    {
        return VerifyFailToRunQueryAfterCompletedTxc(false);
    }

    private async Task VerifyFailToRunQueryAfterCompletedTxc(bool commit)
    {
        var txc = await _session.BeginTransaction().SingleAsync();

        VerifyCanCreateNode(txc, 15);
        VerifyCanCommitOrRollback(txc, commit);

        CountNodes(15).Should().Be(commit ? 1 : 0);

        txc.Run("CREATE ()")
            .Records()
            .WaitForCompletion()
            .AssertEqual(
                OnError<IRecord>(
                    0,
                    Matches<Exception>(exc => exc.Message.Should().Contain("Cannot run query in this"))));
    }

    [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
    public async Task ShouldUpdateBookmark()
    {
        var bookmark1 = _session.LastBookmarks;

        var txc1 = await _session.BeginTransaction().SingleAsync();
        VerifyCanCreateNode(txc1, 20);
        VerifyCanCommit(txc1);

        var bookmark2 = _session.LastBookmarks;

        var txc2 = await _session.BeginTransaction().SingleAsync();
        VerifyCanCreateNode(txc2, 20);
        VerifyCanCommit(txc2);

        var bookmark3 = _session.LastBookmarks;

        bookmark1.Should().BeNull();
        bookmark2.Should().NotBe(bookmark1);
        bookmark3.Should().NotBe(bookmark1).And.NotBe(bookmark2);
    }

    [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
    public async Task ShouldPropagateFailuresFromQuerys()
    {
        var txc = await _session.BeginTransaction().SingleAsync();

        var result1 = txc.Run("CREATE (:TestNode) RETURN 1 as n");
        var result2 = txc.Run("CREATE (:TestNode) RETURN 2 as n");
        var result3 = txc.Run("RETURN 10 / 0 as n");
        var result4 = txc.Run("CREATE (:TestNode) RETURN 3 as n");

        Observable.Concat(
                result1.Records(),
                result2.Records(),
                result3.Records(),
                result4.Records())
            .WaitForCompletion()
            .AssertEqual(
                OnNext(0, MatchesRecord(new[] { "n" }, 1)),
                OnNext(0, MatchesRecord(new[] { "n" }, 2)),
                OnError<IRecord>(0, Matches<Exception>(exc => exc.Message.Should().Contain("/ by zero"))));

        VerifyCanRollback(txc);
    }

    [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
    public async Task ShouldNotRunUntilSubscribed()
    {
        var txc = await _session.BeginTransaction().SingleAsync();

        var result1 = txc.Run("RETURN 1");
        var result2 = txc.Run("RETURN 2");
        var result3 = txc.Run("RETURN 3");
        var result4 = txc.Run("RETURN 4");

        Observable.Concat(
                result4.Records(),
                result3.Records(),
                result2.Records(),
                result1.Records())
            .Select(r => r[0].As<int>())
            .WaitForCompletion()
            .AssertEqual(
                OnNext(0, 4),
                OnNext(0, 3),
                OnNext(0, 2),
                OnNext(0, 1),
                OnCompleted<int>(0));
    }

    [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
    public Task ShouldNotPropagateFailureIfNotExecutedAndCommitted()
    {
        return VerifyNotPropagateFailureIfNotExecuted(true);
    }

    [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
    public Task ShouldNotPropagateFailureIfNotExecutedAndRollBacked()
    {
        return VerifyNotPropagateFailureIfNotExecuted(false);
    }

    private async Task VerifyNotPropagateFailureIfNotExecuted(bool commit)
    {
        var txc = await _session.BeginTransaction().SingleAsync();

        txc.Run("RETURN ILLEGAL");

        VerifyCanCommitOrRollback(txc, commit);
    }

    [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
    public async Task ShouldNotPropagateRunFailureFromSummary()
    {
        var txc = await _session.BeginTransaction().SingleAsync();
        var result = txc.Run("RETURN Wrong");

        result.Records()
            .WaitForCompletion()
            .AssertEqual(OnError<IRecord>(0, MatchesException<ClientException>()));

        result.Consume()
            .WaitForCompletion()
            .AssertEqual(
                OnNext(0, Matches<IResultSummary>(x => x.Should().NotBeNull())),
                OnCompleted<IResultSummary>(0));

        VerifyCanRollback(txc);
    }

    [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
    public void ShouldHandleNestedQueries()
    {
        const int size = 1024;

        var messages = _session.BeginTransaction()
            .SelectMany(
                txc =>
                    txc.Run("UNWIND range(1, $size) AS x RETURN x", new { size })
                        .Records()
                        .Select(r => r[0].As<int>())
                        .Buffer(50)
                        .SelectMany(
                            x =>
                                txc.Run("UNWIND $x AS id CREATE (n:Node {id: id}) RETURN n.id", new { x }).Records())
                        .Select(r => r[0].As<int>())
                        .Concat(txc.Commit<int>())
                        .Catch((Exception exc) => txc.Rollback<int>().Concat(Observable.Throw<int>(exc))))
            .WaitForCompletion()
            .ToList();

        messages.Should()
            .HaveCount(size + 1)
            .And
            .NotContain(n => n.Value.Kind == NotificationKind.OnError);
    }

    private static void VerifyCanCommit(IRxTransaction txc)
    {
        txc.Commit<int>()
            .WaitForCompletion()
            .AssertEqual(OnCompleted<int>(0));
    }

    private static void VerifyCanRollback(IRxTransaction txc)
    {
        txc.Rollback<int>()
            .WaitForCompletion()
            .AssertEqual(OnCompleted<int>(0));
    }

    private static void VerifyCanCommitOrRollback(IRxTransaction txc, bool commit)
    {
        if (commit)
        {
            VerifyCanCommit(txc);
        }
        else
        {
            VerifyCanRollback(txc);
        }
    }

    private static void VerifyCanCreateNode(IRxTransaction txc, int id)
    {
        txc.Run("CREATE (n:Node {id: $id}) RETURN n", new { id })
            .Records()
            .Select(r => r["n"].As<INode>())
            .SingleAsync()
            .WaitForCompletion()
            .AssertEqual(
                OnNext(
                    0,
                    Matches<INode>(
                        node => node.Should()
                            .BeEquivalentTo(
                                new
                                {
                                    Labels = new[] { "Node" },
                                    Properties = new Dictionary<string, object>
                                    {
                                        { "id", (long)id }
                                    }
                                }))),
                OnCompleted<INode>(0));
    }

    private static void VerifyCanReturnOne(IRxTransaction txc)
    {
        txc.Run("RETURN 1")
            .Records()
            .Select(r => r[0].As<int>())
            .WaitForCompletion()
            .AssertEqual(
                OnNext(0, 1),
                OnCompleted<int>(0));
    }

    private static void VerifyFailsWithWrongQuery(IRxTransaction txc)
    {
        txc.Run("RETURN")
            .Records()
            .WaitForCompletion()
            .AssertEqual(OnError<IRecord>(0, MatchesException<ClientException>(e => e.Code.Contains("SyntaxError"))));
    }

    private void VerifyCommittedOrRollbacked(bool commit)
    {
        if (commit)
        {
            CountNodes(1).Should().Be(2);
            CountNodes(2).Should().Be(1);
        }
        else
        {
            CountNodes(1).Should().Be(0);
            CountNodes(2).Should().Be(0);
        }
    }

    private int CountNodes(int id)
    {
        return NewSession()
            .Run("MATCH (n:Node {id: $id}) RETURN count(n)", new { id })
            .Records()
            .Select(r => r[0].As<int>())
            .SingleAsync()
            .Wait();
    }
}
