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
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Tests;
using Xunit;
using Xunit.Abstractions;
using static Microsoft.Reactive.Testing.ReactiveAssert;
using static Neo4j.Driver.Tests.Assertions;

namespace Neo4j.Driver.Reactive.Internal
{
    public static class InternalRxSessionTests
    {
        public class Run : AbstractRxTest
        {
            [Fact]
            public void ShouldReturnInternalRxResult()
            {
                var rxSession = new InternalRxSession(Mock.Of<ISession>());

                rxSession.Run("RETURN 1").Should().BeOfType<InternalRxResult>();
            }

            [Fact]
            public void ShouldInvokeSessionRunAsyncOnKeys()
            {
                VerifyLazyRunAsync(r => r.Keys().WaitForCompletion());
            }

            [Fact]
            public void ShouldInvokeSessionRunAsyncOnRecords()
            {
                VerifyLazyRunAsync(r => r.Records().WaitForCompletion());
            }

            [Fact]
            public void ShouldInvokeSessionRunAsyncOnSummary()
            {
                VerifyLazyRunAsync(r => r.Summary().WaitForCompletion());
            }

            [Fact]
            public void ShouldInvokeSessionRunAsyncOnlyOnce()
            {
                VerifyLazyRunAsync(r =>
                {
                    r.Keys().WaitForCompletion();
                    r.Records().WaitForCompletion();
                    r.Summary().WaitForCompletion();
                });
            }

            private static void VerifyLazyRunAsync(Action<IRxResult> action)
            {
                var asyncSession = new Mock<ISession>();
                asyncSession.Setup(x => x.RunAsync(It.IsAny<Statement>(), It.IsAny<TransactionConfig>()))
                    .ReturnsAsync(new ListBasedRecordCursor(new[] {"x"}, Enumerable.Empty<IRecord>,
                        Mock.Of<IResultSummary>));
                var session = new InternalRxSession(asyncSession.Object);
                var result = session.Run("RETURN 1");

                asyncSession.Verify(
                    x => x.RunAsync(It.IsAny<Statement>(), It.IsAny<TransactionConfig>()), Times.Never);

                action(result);

                asyncSession.Verify(
                    x => x.RunAsync(It.IsAny<Statement>(), It.IsAny<TransactionConfig>()), Times.Once);
            }
        }

        public class BeginTransaction : AbstractRxTest
        {
            [Fact]
            public void ShouldReturnObservable()
            {
                var session = new Mock<ISession>();
                session.Setup(x => x.BeginTransactionAsync(It.IsAny<TransactionConfig>()))
                    .ReturnsAsync(Mock.Of<ITransaction>());

                var rxSession = new InternalRxSession(session.Object);

                rxSession.BeginTransaction().WaitForCompletion()
                    .AssertEqual(
                        OnNext(0, Matches<IRxTransaction>(t => t.Should().BeOfType<InternalRxTransaction>())),
                        OnCompleted<IRxTransaction>(0));
                session.Verify(x => x.BeginTransactionAsync(It.IsAny<TransactionConfig>()), Times.Once);
            }
        }

        public class ReadWriteTransaction : AbstractRxTest
        {
            [Fact]
            public void ShouldThrowOnReadTransaction()
            {
                var rxSession = new InternalRxSession(Mock.Of<ISession>());
                var exc = Record.Exception(() =>
                    rxSession.ReadTransaction(txc => Observable.Empty<string>()));

                exc.Should().BeOfType<NotImplementedException>();
            }

            [Fact]
            public void ShouldThrowOnWriteTransaction()
            {
                var rxSession = new InternalRxSession(Mock.Of<ISession>());
                var exc = Record.Exception(() =>
                    rxSession.WriteTransaction(txc => Observable.Empty<string>()));

                exc.Should().BeOfType<NotImplementedException>();
            }
        }

        public class Close : AbstractRxTest
        {
            [Fact]
            public void ShouldInvokeSessionCloseAsync()
            {
                var asyncSession = new Mock<ISession>();
                var rxSession = new InternalRxSession(asyncSession.Object);

                var close = rxSession.Close<Unit>();

                asyncSession.Verify(x => x.CloseAsync(), Times.Never);

                close.WaitForCompletion();

                asyncSession.Verify(x => x.CloseAsync(), Times.Once);
            }
        }
    }
}