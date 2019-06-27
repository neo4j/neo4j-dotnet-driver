// Copyright (c) 2002-2019 "Neo4j,"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
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
using Xunit;
using static Neo4j.Driver.Reactive.Utils;
using Record = Neo4j.Driver.Internal.Result.Record;

namespace Neo4j.Driver.Reactive.Internal
{
    public static class InternalRxResultTests
    {
        public class Streaming : AbstractRxTest
        {
            [Fact]
            public void ShouldReturnKeys()
            {
                var cursor = CreateResultCursor(3, 0);
                var result = new InternalRxResult(Observable.Return(cursor));

                VerifyKeys(result, "key01", "key02", "key03");
            }

            [Fact]
            public void ShouldReturnKeysAfterRecords()
            {
                var keys = new[] {"key01", "key02", "key03"};
                var cursor = CreateResultCursor(3, 0);
                var result = new InternalRxResult(Observable.Return(cursor));

                VerifyRecords(result, keys, 0);
                VerifyKeys(result, keys);
            }

            [Fact]
            public void ShouldReturnKeysAfterSummary()
            {
                var keys = new[] {"key01", "key02", "key03"};
                var cursor = CreateResultCursor(3, 0);
                var result = new InternalRxResult(Observable.Return(cursor));

                VerifySummary(result);
                VerifyKeys(result, keys);
            }

            [Fact]
            public void ShouldReturnKeysRepeatable()
            {
                var cursor = CreateResultCursor(3, 0);
                var result = new InternalRxResult(Observable.Return(cursor));

                VerifyKeys(result, "key01", "key02", "key03");
                VerifyKeys(result, "key01", "key02", "key03");
                VerifyKeys(result, "key01", "key02", "key03");
                VerifyKeys(result, "key01", "key02", "key03");
            }

            [Fact]
            public void ShouldReturnRecords()
            {
                var keys = new[] {"key01", "key02", "key03"};
                var cursor = CreateResultCursor(3, 5);
                var result = new InternalRxResult(Observable.Return(cursor));

                VerifyRecords(result, keys, 5);
            }

            [Fact]
            public void ShouldNotReturnRecordsIfRecordsIsAlreadyObserved()
            {
                var keys = new[] {"key01", "key02", "key03"};
                var cursor = CreateResultCursor(3, 5);
                var result = new InternalRxResult(Observable.Return(cursor));

                VerifyRecords(result, keys, 5);
                VerifyNoRecords(result);
            }

            [Fact]
            public void ShouldReturnSummary()
            {
                var cursor = CreateResultCursor(3, 0, "my statement");
                var result = new InternalRxResult(Observable.Return(cursor));

                VerifySummary(result, "my statement");
            }

            [Fact]
            public void ShouldNotReturnRecordsIfSummaryIsObserved()
            {
                var cursor = CreateResultCursor(3, 0, "my statement");
                var result = new InternalRxResult(Observable.Return(cursor));

                VerifySummary(result, "my statement");
                VerifyNoRecords(result);
            }

            [Fact]
            public void ShouldReturnSummaryRepeatable()
            {
                var cursor = CreateResultCursor(3, 0, "my statement");
                var result = new InternalRxResult(Observable.Return(cursor));

                VerifySummary(result, "my statement");
                VerifySummary(result, "my statement");
                VerifySummary(result, "my statement");
                VerifySummary(result, "my statement");
            }

            [Fact]
            public void ShouldReturnKeysRecordsAndSummary()
            {
                var keys = new[] {"key01", "key02", "key03"};
                var cursor = CreateResultCursor(3, 5, "my statement");
                var result = new InternalRxResult(Observable.Return(cursor));

                VerifyKeys(result, keys);
                VerifyRecords(result, keys, 5);
                VerifySummary(result, "my statement");
            }

            [Fact]
            public void ShouldReturnRecordsAndSummary()
            {
                var keys = new[] {"key01", "key02", "key03"};
                var cursor = CreateResultCursor(3, 5, "my statement");
                var result = new InternalRxResult(Observable.Return(cursor));

                VerifyRecords(result, keys, 5);
                VerifySummary(result, "my statement");
            }

            [Fact]
            public void ShouldReturnsKeysAndSummary()
            {
                var keys = new[] {"key01", "key02", "key03"};
                var cursor = CreateResultCursor(3, 5, "my statement");
                var result = new InternalRxResult(Observable.Return(cursor));

                VerifyKeys(result, keys);
                VerifySummary(result, "my statement");
            }

            [Fact]
            public void ShouldNotAllowConcurrentRecordObservers()
            {
                var cursor = CreateResultCursor(3, 20, "my statement", 1000);
                var result = new InternalRxResult(Observable.Return(cursor));

                result.Records()
                    .Merge(result.Records())
                    .WaitForCompletion()
                    .AssertEqual(
                        OnError<IRecord>(0, MatchesException<ClientException>()));
            }


            private void VerifyKeys(IRxResult result, params string[] keys)
            {
                result.Keys()
                    .WaitForCompletion()
                    .AssertEqual(
                        OnNext(0, MatchesKeys(keys)),
                        OnCompleted<string[]>(0));
            }

            private void VerifyRecords(IRxResult result, string[] keys, int recordsCount)
            {
                result.Records()
                    .WaitForCompletion()
                    .AssertEqual(
                        Enumerable.Range(1, recordsCount).Select(r =>
                                OnNext(0,
                                    MatchesRecord(keys,
                                        Enumerable.Range(1, keys.Length).Select(f => $"{r:D3}_{f:D2}").Cast<object>()
                                            .ToArray())))
                            .Concat(new[] {OnCompleted<IRecord>(0)}));
            }

            private void VerifyNoRecords(IRxResult result)
            {
                VerifyRecords(result, new string[0], 0);
            }

            private void VerifySummary(IRxResult result, string statement = "fake")
            {
                result.Summary()
                    .WaitForCompletion()
                    .AssertEqual(
                        OnNext(0,
                            MatchesSummary(new {Statement = new Statement(statement)},
                                opts => opts.ExcludingMissingMembers())),
                        OnCompleted<IResultSummary>(0));
            }

            private static IEnumerable<IRecord> CreateRecords(string[] fields, int recordCount, int delayMs = 0)
            {
                for (var i = 1; i <= recordCount; i++)
                {
                    if (delayMs > 0)
                    {
                        Thread.Sleep(delayMs);
                    }

                    yield return new Record(fields,
                        Enumerable.Range(1, fields.Length).Select(f => $"{i:D3}_{f:D2}").Cast<object>().ToArray());
                }
            }

            private static IInternalStatementResultCursor CreateResultCursor(int keyCount, int recordCount,
                string statement = "fake", int delayMs = 0)
            {
                var fields = Enumerable.Range(1, keyCount).Select(f => $"key{f:D2}").ToArray();
                var summaryBuilder =
                    new SummaryBuilder(new Statement(statement), new ServerInfo(new Uri("bolt://localhost")));

                return new ListBasedRecordCursor(fields, () => CreateRecords(fields, recordCount, delayMs),
                    () => summaryBuilder.Build());
            }
        }

        public class CursorKeysErrors : AbstractRxTest
        {
            [Fact]
            public void ShouldErrorOnKeys()
            {
                var exc = new ClientException("some error");
                var cursor = CreateFailingResultCursor(exc);
                var result = new InternalRxResult(Observable.Return(cursor));

                VerifyError(result.Keys(), exc);
            }

            [Fact]
            public void ShouldErrorOnKeysRepeatable()
            {
                var exc = new ClientException("some error");
                var cursor = CreateFailingResultCursor(exc);
                var result = new InternalRxResult(Observable.Return(cursor));

                VerifyError(result.Keys(), exc);
                VerifyError(result.Keys(), exc);
                VerifyError(result.Keys(), exc);
            }

            [Fact]
            public void ShouldErrorOnRecords()
            {
                var exc = new ClientException("some error");
                var cursor = CreateFailingResultCursor(exc);
                var result = new InternalRxResult(Observable.Return(cursor));

                VerifyError(result.Records(), exc);
            }

            [Fact]
            public void ShouldErrorOnRecordsRepeatable()
            {
                var exc = new ClientException("some error");
                var cursor = CreateFailingResultCursor(exc);
                var result = new InternalRxResult(Observable.Return(cursor));

                VerifyError(result.Records(), exc);
                VerifyError(result.Records(), exc);
                VerifyError(result.Records(), exc);
            }

            [Fact]
            public void ShouldErrorOnSummary()
            {
                var exc = new ClientException("some error");
                var cursor = CreateFailingResultCursor(exc);
                var result = new InternalRxResult(Observable.Return(cursor));

                VerifyError(result.Summary(), exc);
            }

            [Fact]
            public void ShouldErrorOnSummaryRepeatable()
            {
                var exc = new ClientException("some error");
                var cursor = CreateFailingResultCursor(exc);
                var result = new InternalRxResult(Observable.Return(cursor));

                VerifyError(result.Summary(), exc);
                VerifyError(result.Summary(), exc);
                VerifyError(result.Summary(), exc);
            }

            [Fact]
            public void ShouldErrorOnKeysRecordsAndButNotOnSummary()
            {
                var exc = new ClientException("some error");
                var cursor = CreateFailingResultCursor(exc);
                var result = new InternalRxResult(Observable.Return(cursor));

                VerifyError(result.Keys(), exc);
                VerifyError(result.Records(), exc);
                VerifyNoError(result.Summary());
            }

            private void VerifyError<T>(IObservable<T> observable, Exception exc)
            {
                observable.WaitForCompletion()
                    .AssertEqual(OnError<T>(0, exc));
            }

            private void VerifyNoError<T>(IObservable<T> observable)
            {
                observable
                    .WaitForCompletion()
                    .Should()
                    .NotContain(e => e.Value.Kind == NotificationKind.OnError).And
                    .Contain(e => e.Value.Kind == NotificationKind.OnCompleted);
            }

            private static IInternalStatementResultCursor CreateFailingResultCursor(Exception exc)
            {
                var cursor = new Mock<IInternalStatementResultCursor>();

                cursor.Setup(x => x.KeysAsync()).ThrowsAsync(exc ?? throw new ArgumentNullException(nameof(exc)));

                return cursor.Object;
            }
        }

        public class CursorFetchErrors : AbstractRxTest
        {
            [Fact]
            public void ShouldReturnKeys()
            {
                var failure = new AuthenticationException("unauthenticated");
                var cursor = CreateFailingResultCursor(failure, 2, 5);
                var result = new InternalRxResult(Observable.Return(cursor));

                VerifyKeys(result, "key01", "key02");
            }

            [Fact]
            public void ShouldErrorOnRecords()
            {
                var keys = new[] {"key01", "key02"};
                var failure = new DatabaseException("code", "message");
                var cursor = CreateFailingResultCursor(failure, 2, 5);
                var result = new InternalRxResult(Observable.Return(cursor));

                VerifyRecordsAndError(result, keys, 5, failure);
            }

            [Fact]
            public void ShouldErrorOnRecordsRepeatable()
            {
                var keys = new[] {"key01", "key02"};
                var failure = new DatabaseException("code", "message");
                var cursor = CreateFailingResultCursor(failure, 2, 5);
                var result = new InternalRxResult(Observable.Return(cursor));

                VerifyRecordsAndError(result, keys, 5, failure);
                VerifyError(result.Records(), failure);
                VerifyError(result.Records(), failure);
            }

            [Fact]
            public void ShouldErrorOnSummary()
            {
                var failure = new DatabaseException("code", "message");
                var cursor = CreateFailingResultCursor(failure, 2, 5);
                var result = new InternalRxResult(Observable.Return(cursor));

                VerifyError(result.Summary(), failure);
            }

            [Fact]
            public void ShouldErrorOnSummaryRepeatable()
            {
                var failure = new DatabaseException("code", "message");
                var cursor = CreateFailingResultCursor(failure, 2, 5);
                var result = new InternalRxResult(Observable.Return(cursor));

                VerifyError(result.Summary(), failure);
                VerifyError(result.Summary(), failure);
                VerifyError(result.Summary(), failure);
            }

            [Fact]
            public void ShouldErrorOnRecordsAndSummary()
            {
                var keys = new[] {"key01", "key02"};
                var failure = new DatabaseException("code", "message");
                var cursor = CreateFailingResultCursor(failure, 2, 5);
                var result = new InternalRxResult(Observable.Return(cursor));

                VerifyRecordsAndError(result, keys, 5, failure);
                VerifyNoError(result.Summary());
            }

            private void VerifyKeys(IRxResult result, params string[] keys)
            {
                result.Keys()
                    .WaitForCompletion()
                    .AssertEqual(
                        OnNext(0, MatchesKeys(keys)),
                        OnCompleted<string[]>(0));
            }

            private void VerifyRecordsAndError(IRxResult result, string[] keys, int recordsCount, Exception failure)
            {
                result.Records()
                    .WaitForCompletion()
                    .AssertEqual(
                        Enumerable.Range(1, recordsCount).Select(r =>
                                OnNext(0,
                                    MatchesRecord(keys,
                                        Enumerable.Range(1, keys.Length).Select(f => $"{r:D3}_{f:D2}").Cast<object>()
                                            .ToArray())))
                            .Concat(new[] {OnError<IRecord>(0, failure)}));
            }

            private void VerifyError<T>(IObservable<T> observable, Exception exc)
            {
                observable.WaitForCompletion()
                    .AssertEqual(
                        OnError<T>(0, exc));
            }

            private void VerifyNoError<T>(IObservable<T> observable)
            {
                observable
                    .WaitForCompletion()
                    .Should()
                    .NotContain(e => e.Value.Kind == NotificationKind.OnError).And
                    .Contain(e => e.Value.Kind == NotificationKind.OnCompleted);
            }

            private static IInternalStatementResultCursor CreateFailingResultCursor(Exception exc, int keyCount,
                int recordCount)
            {
                var keys = Enumerable.Range(1, keyCount).Select(f => $"key{f:D2}").ToArray();

                IEnumerable<IRecord> GenerateRecords()
                {
                    for (var r = 1; r <= recordCount; r++)
                    {
                        yield return new Record(keys,
                            Enumerable.Range(1, keyCount).Select(f => $"{r:D3}_{f:D2}").Cast<object>().ToArray());
                    }

                    throw exc;
                }

                return new ListBasedRecordCursor(keys, GenerateRecords);
            }
        }

        public class CursorSummaryErrors : AbstractRxTest
        {
            [Fact]
            public void ShouldReturnKeys()
            {
                var failure = new AuthenticationException("unauthenticated");
                var cursor = CreateFailingResultCursor(failure, 2, 5);
                var result = new InternalRxResult(Observable.Return(cursor));

                VerifyKeys(result, "key01", "key02");
            }

            [Fact]
            public void ShouldReturnRecords()
            {
                var keys = new[] {"key01", "key02"};
                var failure = new DatabaseException("code", "message");
                var cursor = CreateFailingResultCursor(failure, 2, 5);
                var result = new InternalRxResult(Observable.Return(cursor));

                VerifyRecords(result, keys, 5);
            }

            [Fact]
            public void ShouldErrorOnSummary()
            {
                var failure = new ClientException("some error");
                var cursor = CreateFailingResultCursor(failure, 2, 5);
                var result = new InternalRxResult(Observable.Return(cursor));

                VerifyError(result.Summary(), failure);
            }

            [Fact]
            public void ShouldErrorOnSummaryRepeatable()
            {
                var failure = new ClientException("some error");
                var cursor = CreateFailingResultCursor(failure, 2, 5);
                var result = new InternalRxResult(Observable.Return(cursor));

                VerifyError(result.Summary(), failure);
                VerifyError(result.Summary(), failure);
            }

            [Fact]
            public void ShouldReturnKeysEvenAfterFailedSummary()
            {
                var failure = new AuthenticationException("unauthenticated");
                var cursor = CreateFailingResultCursor(failure, 2, 5);
                var result = new InternalRxResult(Observable.Return(cursor));

                VerifyError(result.Summary(), failure);
                VerifyKeys(result, "key01", "key02");
            }

            private void VerifyKeys(IRxResult result, params string[] keys)
            {
                result.Keys()
                    .WaitForCompletion()
                    .AssertEqual(
                        OnNext(0, MatchesKeys(keys)),
                        OnCompleted<string[]>(0));
            }

            private void VerifyRecords(IRxResult result, string[] keys, int recordsCount)
            {
                result.Records()
                    .WaitForCompletion()
                    .AssertEqual(
                        Enumerable.Range(1, recordsCount).Select(r =>
                                OnNext(0,
                                    MatchesRecord(keys,
                                        Enumerable.Range(1, keys.Length).Select(f => $"{r:D3}_{f:D2}").Cast<object>()
                                            .ToArray())))
                            .Concat(new[] {OnCompleted<IRecord>(0)}));
            }

            private void VerifyError<T>(IObservable<T> observable, Exception exc)
            {
                observable.WaitForCompletion()
                    .AssertEqual(OnError<T>(0, exc));
            }

            private static IInternalStatementResultCursor CreateFailingResultCursor(Exception exc, int keyCount,
                int recordCount)
            {
                var fields = Enumerable.Range(1, keyCount).Select(f => $"key{f:D2}").ToArray();
                var records = Enumerable.Range(1, recordCount).Select(
                    r => new Record(fields,
                        Enumerable.Range(1, keyCount).Select(f => $"{r:D3}_{f:D2}").Cast<object>().ToArray())
                );

                return new ListBasedRecordCursor(fields, () => records, () => throw exc);
            }
        }
    }
}