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

using System;
using System.Linq;
using System.Reactive.Linq;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Tests.Reactive.Utils;
using Xunit;
using static Neo4j.Driver.Tests.TestUtil.Assertions;

namespace Neo4j.Driver.Tests.Reactive.Internal;

public class RxRetryLogicTests : AbstractRxTest
{
    [Theory]
    [MemberData(nameof(NonTransientErrors))]
    public void ShouldNotRetryOnNonTransientErrors(Exception error)
    {
        var retryLogic = new RxRetryLogic(TimeSpan.FromSeconds(5), null);

        var observable =
            Scheduler.CreateColdObservable(
                OnNext(100, 1),
                OnNext(150, 2),
                OnNext(250, 3),
                OnError<int>(350, error));

        var observer = Scheduler.Start(
            () =>
                retryLogic.Retry(observable),
            0,
            100,
            500);

        observer.Messages.AssertEqual(
            OnNext(200, 1),
            OnNext(250, 2),
            OnNext(350, 3),
            OnError<int>(450, error));
    }

    [Theory]
    [MemberData(nameof(TransientErrors))]
    public void ShouldRetryOnTransientErrors(Exception error)
    {
        var retryLogic = new RxRetryLogic(TimeSpan.FromSeconds(5), null);

        retryLogic
            .Retry(CreateFailingObservable(1, error))
            .WaitForCompletion()
            .AssertEqual(
                OnNext(0, 1),
                OnCompleted<int>(0));
    }

    [Fact]
    public void ShouldNotRetryOnSuccess()
    {
        var retryLogic = new RxRetryLogic(TimeSpan.FromSeconds(5), null);

        retryLogic
            .Retry(Observable.Return(5))
            .WaitForCompletion()
            .AssertEqual(
                OnNext(0, 5),
                OnCompleted<int>(0));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    public void ShouldLogRetries(int errorCount)
    {
        var error = new TransientException("code", "message");
        var logger = new Mock<ILogger>();
        var retryLogic = new RxRetryLogic(TimeSpan.FromMinutes(1), logger.Object);

        retryLogic
            .Retry(
                CreateFailingObservable(
                    1,
                    Enumerable.Range(1, errorCount).Select(_ => error).Cast<Exception>().ToArray()))
            .WaitForCompletion()
            .AssertEqual(
                OnNext(0, 1),
                OnCompleted<int>(0));

        logger.Verify(
            x => x.Warn(
                error,
                It.Is<string>(s => s.StartsWith("Transaction failed and will be retried in"))),
            Times.Exactly(errorCount));
    }

    [Fact]
    public void ShouldRetryAtLeastTwice()
    {
        var error = new TransientException("code", "message");
        var logger = new Mock<ILogger>();
        var retryLogic = new RxRetryLogic(TimeSpan.FromSeconds(1), logger.Object);

        retryLogic
            .Retry(CreateFailingObservable(TimeSpan.FromSeconds(2), 1, error))
            .WaitForCompletion()
            .AssertEqual(
                OnNext(0, 1),
                OnCompleted<int>(0));

        logger.Verify(
            x => x.Warn(
                error,
                It.Is<string>(s => s.StartsWith("Transaction failed and will be retried in"))),
            Times.Once);
    }

    [Fact]
    public void ShouldThrowServiceUnavailableWhenRetriesTimedOut()
    {
        var errorCount = 3;
        var exceptions = Enumerable.Range(1, errorCount)
            .Select(i => new TransientException($"{i}", $"{i}"))
            .Cast<Exception>()
            .ToArray();

        var logger = new Mock<ILogger>();
        var retryLogic = new RxRetryLogic(TimeSpan.FromSeconds(2), logger.Object);

        retryLogic
            .Retry(CreateFailingObservable(TimeSpan.FromSeconds(1), 1, exceptions))
            .WaitForCompletion()
            .AssertEqual(
                OnError<int>(
                    0,
                    e => Matches(
                        () =>
                            e.Should()
                                .BeOfType<ServiceUnavailableException>()
                                .Which.InnerException.Should()
                                .BeOfType<AggregateException>()
                                .Which.InnerExceptions.Should()
                                .BeSubsetOf(exceptions))));
    }

    private static IObservable<T> CreateFailingObservable<T>(T success, params Exception[] exceptions)
    {
        return CreateFailingObservable(TimeSpan.Zero, success, exceptions);
    }

    private static IObservable<T> CreateFailingObservable<T>(
        TimeSpan delay,
        T success,
        params Exception[] exceptions)
    {
        var index = 0;

        return Observable.Defer(
            () =>
                index < exceptions.Length
                    ? Observable.Throw<T>(exceptions[index++]).Delay(delay)
                    : Observable.Return(success).Delay(delay));
    }

    public static TheoryData<Exception> NonTransientErrors()
    {
        return new TheoryData<Exception>
        {
            new ArgumentOutOfRangeException("error"),
            new ClientException("invalid"),
            new InvalidOperationException("invalid operation"),
            new DatabaseException("Neo.TransientError.Transaction.Terminated", "transaction terminated"),
            new DatabaseException("Neo.TransientError.Transaction.LockClientStopped", "lock client stopped")
        };
    }

    public static TheoryData<Exception> TransientErrors()
    {
        return new TheoryData<Exception>
        {
            new TransientException("Neo.TransientError.Database.Unavailable", "database unavailable"),
            new SessionExpiredException("session expired"),
            new ServiceUnavailableException("service unavailable")
        };
    }
}
