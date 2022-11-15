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
using System.Threading;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.Tests;
using Neo4j.Driver.TestUtil;
using Xunit;
using Xunit.Abstractions;
using static Microsoft.Reactive.Testing.ReactiveTest;
using static Neo4j.Driver.Reactive.Internal.InternalRxResultTests.RxResultUtil;
using static Neo4j.Driver.Reactive.Utils;
using Record = Neo4j.Driver.Internal.Result.Record;

namespace Neo4j.Driver.Reactive.Internal;

public static class InternalRxResultTests
{
    public class Streaming : AbstractRxTest
    {
        [Fact]
        public void ShouldReturnKeys()
        {
            var cursor = CreateResultCursor(3, 0);
            var result = new RxResult(Observable.Return(cursor));

            VerifyKeys(result, "key01", "key02", "key03");
        }

        [Fact]
        public void ShouldReturnKeysAfterRecords()
        {
            var keys = new[] { "key01", "key02", "key03" };
            var cursor = CreateResultCursor(3, 0);
            var result = new RxResult(Observable.Return(cursor));

            VerifyRecords(result, keys, 0);
            VerifyKeys(result, keys);
        }

        [Fact]
        public void ShouldReturnKeysAfterSummary()
        {
            var keys = new[] { "key01", "key02", "key03" };
            var cursor = CreateResultCursor(3, 0);
            var result = new RxResult(Observable.Return(cursor));

            VerifySummary(result);
            VerifyKeys(result, keys);
        }

        [Fact]
        public void ShouldReturnKeysRepeatable()
        {
            var cursor = CreateResultCursor(3, 0);
            var result = new RxResult(Observable.Return(cursor));

            VerifyKeys(result, "key01", "key02", "key03");
            VerifyKeys(result, "key01", "key02", "key03");
            VerifyKeys(result, "key01", "key02", "key03");
            VerifyKeys(result, "key01", "key02", "key03");
        }

        [Fact]
        public void ShouldReturnRecords()
        {
            var keys = new[] { "key01", "key02", "key03" };
            var cursor = CreateResultCursor(3, 5);
            var result = new RxResult(Observable.Return(cursor));

            VerifyRecords(result, keys, 5);
        }

        [Fact]
        public void ShouldNotReturnRecordsIfRecordsIsAlreadyObserved()
        {
            var keys = new[] { "key01", "key02", "key03" };
            var cursor = CreateResultCursor(3, 5);
            var result = new RxResult(Observable.Return(cursor));

            VerifyRecords(result, keys, 5);
            VerifyResultConsumedError(result.Records());
        }

        [Fact]
        public void ShouldReturnSummary()
        {
            var cursor = CreateResultCursor(3, 0, "my query");
            var result = new RxResult(Observable.Return(cursor));

            VerifySummary(result, "my query");
        }

        [Fact]
        public void ShouldNotReturnRecordsIfSummaryIsObserved()
        {
            var cursor = CreateResultCursor(3, 0, "my query");
            var result = new RxResult(Observable.Return(cursor));

            VerifySummary(result, "my query");
            VerifyResultConsumedError(result.Records());
        }

        [Fact]
        public void ShouldReturnSummaryRepeatable()
        {
            var cursor = CreateResultCursor(3, 0, "my query");
            var result = new RxResult(Observable.Return(cursor));

            VerifySummary(result, "my query");
            VerifySummary(result, "my query");
            VerifySummary(result, "my query");
            VerifySummary(result, "my query");
        }

        [Fact]
        public void ShouldReturnKeysRecordsAndSummary()
        {
            var keys = new[] { "key01", "key02", "key03" };
            var cursor = CreateResultCursor(3, 5, "my query");
            var result = new RxResult(Observable.Return(cursor));

            VerifyKeys(result, keys);
            VerifyRecords(result, keys, 5);
            VerifySummary(result, "my query");
        }

        [Fact]
        public void ShouldReturnRecordsAndSummary()
        {
            var keys = new[] { "key01", "key02", "key03" };
            var cursor = CreateResultCursor(3, 5, "my query");
            var result = new RxResult(Observable.Return(cursor));

            VerifyRecords(result, keys, 5);
            VerifySummary(result, "my query");
        }

        [Fact]
        public void ShouldReturnsKeysAndSummary()
        {
            var keys = new[] { "key01", "key02", "key03" };
            var cursor = CreateResultCursor(3, 5, "my query");
            var result = new RxResult(Observable.Return(cursor));

            VerifyKeys(result, keys);
            VerifySummary(result, "my query");
        }

        [Fact]
        public void ShouldNotAllowConcurrentRecordObservers()
        {
            var cursor = CreateResultCursor(3, 20, "my query", 1000);
            var result = new RxResult(Observable.Return(cursor));

            result.Records()
                .Merge(result.Records())
                .WaitForCompletion()
                .AssertEqual(OnError<IRecord>(0, MatchesException<ClientException>()));
        }
    }

    public class CursorKeysErrors : AbstractRxTest
    {
        public CursorKeysErrors(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void ShouldErrorOnKeys()
        {
            var exc = new ClientException("some error");
            var cursor = CreateFailingResultCursor(exc);
            var result = new RxResult(Observable.Return(cursor));

            VerifyError(result.Keys(), exc);
        }

        [Fact]
        public void ShouldErrorOnKeysRepeatable()
        {
            var exc = new ClientException("some error");
            var cursor = CreateFailingResultCursor(exc);
            var result = new RxResult(Observable.Return(cursor));

            VerifyError(result.Keys(), exc);
            VerifyError(result.Keys(), exc);
            VerifyError(result.Keys(), exc);
        }

        [Fact]
        public void ShouldErrorOnRecords()
        {
            var exc = new ClientException("some error");
            var cursor = CreateFailingResultCursor(exc);
            var result = new RxResult(Observable.Return(cursor));

            VerifyError(result.Records(), exc);
        }

        [Fact]
        public void ShouldErrorOnRecordsRepeatable()
        {
            var exc = new ClientException("some error");
            var cursor = CreateFailingResultCursor(exc);
            var result = new RxResult(Observable.Return(cursor), new TestLogger(Output.WriteLine));

            VerifyError(result.Records(), exc);

            VerifyResultConsumedError(result.Records());
            VerifyResultConsumedError(result.Records());
        }

        [Fact]
        public void ShouldErrorOnSummary()
        {
            var exc = new ClientException("some error");
            var cursor = CreateFailingResultCursor(exc);
            var result = new RxResult(Observable.Return(cursor));

            VerifyError(result.Consume(), exc);
        }

        [Fact]
        public void ShouldErrorOnSummaryRepeatable()
        {
            var exc = new ClientException("some error");
            var cursor = CreateFailingResultCursor(exc);
            var result = new RxResult(Observable.Return(cursor));

            VerifyError(result.Consume(), exc);
            VerifyError(result.Consume(), exc);
            VerifyError(result.Consume(), exc);
        }

        [Fact]
        public void ShouldErrorOnKeysRecordsAndButNotOnSummary()
        {
            var exc = new ClientException("some error");
            var cursor = CreateFailingResultCursor(exc);
            var result = new RxResult(Observable.Return(cursor));

            VerifyError(result.Keys(), exc);
            VerifyError(result.Records(), exc);
            VerifyNoError(result.Consume());
        }
    }

    public class CursorFetchErrors : AbstractRxTest
    {
        [Fact]
        public void ShouldReturnKeys()
        {
            var failure = new AuthenticationException("unauthenticated");
            var cursor = CreateFailingResultCursor(failure, 2, 5);
            var result = new RxResult(Observable.Return(cursor));

            VerifyKeys(result, "key01", "key02");
        }

        [Fact]
        public void ShouldErrorOnRecords()
        {
            var keys = new[] { "key01", "key02" };
            var failure = new DatabaseException("code", "message");
            var cursor = CreateFailingResultCursor(failure, 2, 5);
            var result = new RxResult(Observable.Return(cursor));

            VerifyRecordsAndError(result, keys, 5, failure);
        }

        [Fact]
        public void ShouldErrorOnRecordsRepeatable()
        {
            var keys = new[] { "key01", "key02" };
            var failure1 = new DatabaseException("code", "message");
            var cursor = CreateFailingResultCursor(failure1, 2, 5);
            var result = new RxResult(Observable.Return(cursor));

            VerifyRecordsAndError(result, keys, 5, failure1);
            VerifyResultConsumedError(result.Records());
            VerifyResultConsumedError(result.Records());
        }

        [Fact]
        public void ShouldErrorOnSummary()
        {
            var failure = new DatabaseException("code", "message");
            var cursor = CreateFailingResultCursor(failure, 2, 5);
            var result = new RxResult(Observable.Return(cursor));

            VerifyError(result.Consume(), failure);
        }

        [Fact]
        public void ShouldErrorOnSummaryRepeatable()
        {
            var failure = new DatabaseException("code", "message");
            var cursor = CreateFailingResultCursor(failure, 2, 5);
            var result = new RxResult(Observable.Return(cursor));

            VerifyError(result.Consume(), failure);
            VerifyError(result.Consume(), failure);
            VerifyError(result.Consume(), failure);
        }

        [Fact]
        public void ShouldErrorOnRecordsAndSummary()
        {
            var keys = new[] { "key01", "key02" };
            var failure = new DatabaseException("code", "message");
            var cursor = CreateFailingResultCursor(failure, 2, 5);
            var result = new RxResult(Observable.Return(cursor));

            VerifyRecordsAndError(result, keys, 5, failure);
            VerifyNoError(result.Consume());
        }
    }

    public class CursorSummaryErrors : AbstractRxTest
    {
        [Fact]
        public void ShouldReturnKeys()
        {
            var failure = new AuthenticationException("unauthenticated");
            var cursor = CreateFailingResultCursor(failure, 2, 5);
            var result = new RxResult(Observable.Return(cursor));

            VerifyKeys(result, "key01", "key02");
        }

        [Fact]
        public void ShouldReturnRecords()
        {
            var keys = new[] { "key01", "key02" };
            var failure = new DatabaseException("code", "message");
            var cursor = CreateFailingResultCursor(failure, 2, 5);
            var result = new RxResult(Observable.Return(cursor));

            VerifyRecordsAndError(result, keys, 5, failure);
        }

        [Fact]
        public void ShouldErrorOnSummary()
        {
            var failure = new ClientException("some error");
            var cursor = CreateFailingResultCursor(failure, 2, 5);
            var result = new RxResult(Observable.Return(cursor));

            VerifyError(result.Consume(), failure);
        }

        [Fact]
        public void ShouldErrorOnSummaryRepeatable()
        {
            var failure = new ClientException("some error");
            var cursor = CreateFailingResultCursor(failure, 2, 5);
            var result = new RxResult(Observable.Return(cursor));

            VerifyError(result.Consume(), failure);
            VerifyError(result.Consume(), failure);
        }

        [Fact]
        public void ShouldReturnKeysEvenAfterFailedSummary()
        {
            var failure = new AuthenticationException("unauthenticated");
            var cursor = CreateFailingResultCursor(failure, 2, 5);
            var result = new RxResult(Observable.Return(cursor));

            VerifyError(result.Consume(), failure);
            VerifyKeys(result, "key01", "key02");
        }
    }

    internal static class RxResultUtil
    {
        public static IEnumerable<IRecord> CreateRecords(string[] fields, int recordCount, int delayMs = 0)
        {
            for (var i = 1; i <= recordCount; i++)
            {
                if (delayMs > 0)
                {
                    Thread.Sleep(delayMs);
                }

                yield return new Record(
                    fields,
                    Enumerable.Range(1, fields.Length).Select(f => $"{i:D3}_{f:D2}").Cast<object>().ToArray());
            }
        }

        public static IInternalResultCursor CreateResultCursor(
            int keyCount,
            int recordCount,
            string query = "fake",
            int delayMs = 0)
        {
            var fields = Enumerable.Range(1, keyCount).Select(f => $"key{f:D2}").ToArray();
            var summaryBuilder =
                new SummaryBuilder(new Query(query), new ServerInfo(new Uri("bolt://localhost")));

            return new ListBasedRecordCursor(
                fields,
                () => CreateRecords(fields, recordCount, delayMs),
                () => summaryBuilder.Build());
        }

        public static void VerifyKeys(IRxResult result, params string[] keys)
        {
            result.Keys()
                .WaitForCompletion()
                .AssertEqual(
                    OnNext(0, MatchesKeys(keys)),
                    OnCompleted<string[]>(0));
        }

        public static void VerifyRecords(IRxResult result, string[] keys, int recordsCount)
        {
            result.Records()
                .WaitForCompletion()
                .AssertEqual(
                    Enumerable.Range(1, recordsCount)
                        .Select(
                            r =>
                                OnNext(
                                    0,
                                    MatchesRecord(
                                        keys,
                                        Enumerable.Range(1, keys.Length)
                                            .Select(f => $"{r:D3}_{f:D2}")
                                            .Cast<object>()
                                            .ToArray())))
                        .Concat(new[] { OnCompleted<IRecord>(0) }));
        }

        public static void VerifyRecordsAndError(
            IRxResult result,
            string[] keys,
            int recordsCount,
            Exception failure)
        {
            result.Records()
                .WaitForCompletion()
                .AssertEqual(
                    Enumerable.Range(1, recordsCount)
                        .Select(
                            r =>
                                OnNext(
                                    0,
                                    MatchesRecord(
                                        keys,
                                        Enumerable.Range(1, keys.Length)
                                            .Select(f => $"{r:D3}_{f:D2}")
                                            .Cast<object>()
                                            .ToArray())))
                        .Concat(new[] { OnError<IRecord>(0, failure) }));
        }

        public static void VerifySummary(IRxResult result, string query = "fake")
        {
            result.Consume()
                .WaitForCompletion()
                .AssertEqual(
                    OnNext(
                        0,
                        MatchesSummary(
                            new { Query = new Query(query) },
                            opts => opts.ExcludingMissingMembers())),
                    OnCompleted<IResultSummary>(0));
        }

        public static void VerifyError<T>(IObservable<T> observable, Exception exc)
        {
            observable.WaitForCompletion()
                .AssertEqual(OnError<T>(0, exc));
        }

        public static void VerifyResultConsumedError<T>(IObservable<T> observable)
        {
            observable.WaitForCompletion()
                .AssertEqual(
                    OnError<T>(
                        0,
                        MatchesException<ResultConsumedException>(
                            e =>
                                e.Message.StartsWith("Streaming has already started and/or finished"))));
        }

        public static void VerifyNoError<T>(IObservable<T> observable)
        {
            observable
                .WaitForCompletion()
                .Should()
                .NotContain(e => e.Value.Kind == NotificationKind.OnError)
                .And
                .Contain(e => e.Value.Kind == NotificationKind.OnCompleted);
        }

        public static IInternalResultCursor CreateFailingResultCursor(Exception exc)
        {
            var cursor = new Mock<IInternalResultCursor>();

            cursor.Setup(x => x.KeysAsync()).ThrowsAsync(exc ?? throw new ArgumentNullException(nameof(exc)));

            return cursor.Object;
        }

        public static IInternalResultCursor CreateFailingResultCursor(
            Exception exc,
            int keyCount,
            int recordCount)
        {
            var keys = Enumerable.Range(1, keyCount).Select(f => $"key{f:D2}").ToArray();

            IEnumerable<IRecord> GenerateRecords()
            {
                for (var r = 1; r <= recordCount; r++)
                {
                    yield return new Record(
                        keys,
                        Enumerable.Range(1, keyCount).Select(f => $"{r:D3}_{f:D2}").Cast<object>().ToArray());
                }

                throw exc;
            }

            return new ListBasedRecordCursor(keys, GenerateRecords);
        }
    }
}
