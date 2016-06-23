// Copyright (c) 2002-2016 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
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
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.V1;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.Tests
{
    public class SessionPoolTests
    {
        private static Uri TestUri => new Uri("bolt://localhost");

        public class GetSessionMethod
        {
            private readonly ITestOutputHelper _output;

            public GetSessionMethod(ITestOutputHelper output)
            {
                _output = output;
            }

            [Fact]
            public void ShouldNotThrowExceptionWhenIdlePoolSizeReached()
            {
                var mock = new Mock<IConnection>();
                var config = new Config {MaxIdleSessionPoolSize = 2};
                var pool = new SessionPool(TestUri, AuthTokens.None, null, config, mock.Object);
                pool.GetSession();
                pool.GetSession();
                pool.NumberOfAvailableSessions.Should().Be(0);
                pool.NumberOfInUseSessions.Should().Be(2);

                var ex = Record.Exception(() => pool.GetSession());
                ex.Should().BeNull();
            }

            [Fact]
            public void ShouldNotExceedIdleLimit()
            {
                var mock = new Mock<IConnection>();
                mock.Setup(x => x.IsOpen).Returns(true);

                var config = new Config {MaxIdleSessionPoolSize = 2};
                var pool = new SessionPool(TestUri, AuthTokens.None, null, config, mock.Object);

                var sessions = new List<ISession>();
                for (var i = 0; i < 4; i++)
                {
                    sessions.Add(pool.GetSession());
                    pool.NumberOfAvailableSessions.Should().BeLessOrEqualTo(2);
                }

                foreach (var session in sessions)
                {
                    session.Dispose();
                    pool.NumberOfAvailableSessions.Should().BeLessOrEqualTo(2);
                }

                pool.NumberOfAvailableSessions.Should().Be(2);
            }

            [Fact]
            public void ShouldGiveSessionsFromPoolIfAvailable()
            {
                var mock = new Mock<IConnection>();
                mock.Setup(x => x.IsOpen).Returns(true);

                var config = new Config {MaxIdleSessionPoolSize = 2};
                var pool = new SessionPool(TestUri, AuthTokens.None, null, config, mock.Object);

                for (var i = 0; i < 4; i++)
                {
                    var session = pool.GetSession();
                    pool.NumberOfAvailableSessions.Should().Be(0);
                    session.Dispose();
                    pool.NumberOfAvailableSessions.Should().Be(1);
                }

                pool.NumberOfAvailableSessions.Should().Be(1);
            }

            [Fact]
            public void ShouldCreateNewSessionWhenQueueIsEmpty()
            {
                var mock = new Mock<IConnection>();
                var pool = new SessionPool(TestUri, AuthTokens.None, null, Config.DefaultConfig, mock.Object);

                pool.GetSession();
                pool.NumberOfAvailableSessions.Should().Be(0);
                pool.NumberOfInUseSessions.Should().Be(1);
            }

            [Fact]
            public void ShouldCreateNewSessionWhenQueueOnlyContainsUnhealthySessions()
            {
                var mock = new Mock<IConnection>();
                var sessions = new Queue<IPooledSession>();
                var unhealthyId = Guid.NewGuid();
                var unhealthyMock = new Mock<IPooledSession>();
                unhealthyMock.Setup(x => x.IsHealthy).Returns(false);
                unhealthyMock.Setup(x => x.Id).Returns(unhealthyId);

                sessions.Enqueue(unhealthyMock.Object);
                var pool = new SessionPool(sessions, null, mock.Object);

                pool.NumberOfAvailableSessions.Should().Be(1);
                pool.NumberOfInUseSessions.Should().Be(0);

                var session = pool.GetSession();

                pool.NumberOfAvailableSessions.Should().Be(0);
                pool.NumberOfInUseSessions.Should().Be(1);
                unhealthyMock.Verify(x => x.Reset(), Times.Never);
                unhealthyMock.Verify(x => x.Close(), Times.Once);

                session.Should().NotBeNull();
                ((IPooledSession) session).Id.Should().NotBe(unhealthyId);
            }

            [Fact]
            public void ShouldCreateNewSessionWhenQueueOnlyContainsUnResetableSessions()
            {
                var mock = new Mock<IConnection>();
                var sessions = new Queue<IPooledSession>();
                var unhealthyId = Guid.NewGuid();
                var unhealthyMock = new Mock<IPooledSession>();
                unhealthyMock.Setup(x => x.IsHealthy).Returns(true);
                unhealthyMock.Setup(x => x.Reset()).Throws<Exception>(); //failed to reset
                unhealthyMock.Setup(x => x.Id).Returns(unhealthyId);

                sessions.Enqueue(unhealthyMock.Object);
                var pool = new SessionPool(sessions, null, mock.Object);

                pool.NumberOfAvailableSessions.Should().Be(1);
                pool.NumberOfInUseSessions.Should().Be(0);

                var session = pool.GetSession();

                pool.NumberOfAvailableSessions.Should().Be(0);
                pool.NumberOfInUseSessions.Should().Be(1);
                unhealthyMock.Verify(x => x.Reset(), Times.Once);
                unhealthyMock.Verify(x => x.Close(), Times.Once);

                session.Should().NotBeNull();
                ((IPooledSession)session).Id.Should().NotBe(unhealthyId);
            }

            [Fact]
            public void ShouldReuseOldSessionWhenReusableSessionInQueue()
            {
                var sessions = new Queue<IPooledSession>();
                var mock = new Mock<IPooledSession>();
                mock.Setup(x => x.IsHealthy).Returns(true);

                sessions.Enqueue(mock.Object);
                var pool = new SessionPool(sessions, null);

                pool.NumberOfAvailableSessions.Should().Be(1);
                pool.NumberOfInUseSessions.Should().Be(0);

                var session = pool.GetSession();

                pool.NumberOfAvailableSessions.Should().Be(0);
                pool.NumberOfInUseSessions.Should().Be(1);
                mock.Verify(x => x.Reset(), Times.Once);
                session.Should().Be(mock.Object);
            }

            [Fact]
            public void ShouldReuseReusableSessionWhenReusableSessionInQueue()
            {
                var sessions = new Queue<IPooledSession>();
                var healthyMock = new Mock<IPooledSession>();
                healthyMock.Setup(x => x.IsHealthy).Returns(true);
                var unhealthyMock = new Mock<IPooledSession>();
                unhealthyMock.Setup(x => x.IsHealthy).Returns(false);

                sessions.Enqueue(unhealthyMock.Object);
                sessions.Enqueue(healthyMock.Object);
                var pool = new SessionPool(sessions, null);

                pool.NumberOfAvailableSessions.Should().Be(2);
                pool.NumberOfInUseSessions.Should().Be(0);

                var session = pool.GetSession();

                pool.NumberOfAvailableSessions.Should().Be(0);
                pool.NumberOfInUseSessions.Should().Be(1);
                unhealthyMock.Verify(x => x.Reset(), Times.Never);
                unhealthyMock.Verify(x => x.Close(), Times.Once);
                healthyMock.Verify(x => x.Reset(), Times.Once);
                session.Should().Be(healthyMock.Object);
            }

            [Theory]
            [InlineData(1)]
            [InlineData(2)]
            [InlineData(5)]
            [InlineData(10)]
            [InlineData(500)]
            public void ShouldGetNewSessionsWhenBeingUsedConcurrentlyBy(int numberOfThreads)
            {
                var ids = new List<Guid>();
                for (var i = 0; i < numberOfThreads; i++)
                {
                    ids.Add(Guid.NewGuid());
                }

                var mockSessions = new Queue<Mock<IPooledSession>>();
                var sessions = new Queue<IPooledSession>();
                for (var i = 0; i < numberOfThreads; i++)
                {
                    var mock = new Mock<IPooledSession>();
                    mock.Setup(x => x.IsHealthy).Returns(true);
                    mock.Setup(x => x.Id).Returns(ids[i]);
                    sessions.Enqueue(mock.Object);
                    mockSessions.Enqueue(mock);
                }

                var pool = new SessionPool(sessions, null);

                pool.NumberOfAvailableSessions.Should().Be(numberOfThreads);
                pool.NumberOfInUseSessions.Should().Be(0);

                var receivedIds = new List<Guid>();

                var tasks = new Task[numberOfThreads];
                for (var i = 0; i < numberOfThreads; i++)
                {
                    var localI = i;
                    tasks[localI] =
                        Task.Run(() =>
                        {
                            try
                            {
                                Task.Delay(500);
                                var session = pool.GetSession();
                                lock (receivedIds)
                                    receivedIds.Add(((IPooledSession) session).Id);
                            }
                            catch (Exception ex)
                            {
                                _output.WriteLine($"Task[{localI}] died: {ex}");
                            }
                        });
                }

                Task.WaitAll(tasks);

                receivedIds.Count.Should().Be(numberOfThreads);
                foreach (var receivedId in receivedIds)
                {
                    receivedIds.Should().ContainSingle(x => x == receivedId);
                    ids.Should().Contain(receivedId);
                }

                foreach (var mock in mockSessions)
                {
                    mock.Verify(x => x.Reset(), Times.Once);
                }
            }

            // thread-safe test
            // concurrent call of GetSession and Dispose
            [Fact]
            public void ShouldCloseSessionGotFromAvailableIfPoolDisposeStarted()
            {
                var sessions = new Queue<IPooledSession>();
                var healthyMock = new Mock<IPooledSession>();
                healthyMock.Setup(x => x.IsHealthy).Returns(true);

                var pool = new SessionPool(sessions, null);

                pool.NumberOfAvailableSessions.Should().Be(0);
                pool.NumberOfInUseSessions.Should().Be(0);

                // this is to simulate we call GetSession after available.clear in pool.Dispose()
                pool.Dispose();
                sessions.Enqueue(healthyMock.Object);
                pool.NumberOfAvailableSessions.Should().Be(1);
                var exception = Record.Exception(() => pool.GetSession());

                pool.NumberOfAvailableSessions.Should().Be(0);
                pool.NumberOfInUseSessions.Should().Be(0);
                healthyMock.Verify(x => x.Reset(), Times.Once);
                healthyMock.Verify(x => x.Close(), Times.Once);
                exception.Should().BeOfType<InvalidOperationException>();
                exception.Message.Should().Contain("the SessionPool is already started to dispose");
            }

            // thread-safe test
            // concurrent call of GetSession and Dispose
            [Fact]
            public void ShouldCloseNewSessionIfPoolDisposeStarted()
            {
                var mock = new Mock<IConnection>();
                var pool = new SessionPool(TestUri, AuthTokens.None, null, Config.DefaultConfig, mock.Object);

                pool.NumberOfAvailableSessions.Should().Be(0);
                pool.NumberOfInUseSessions.Should().Be(0);

                // this is to simulate we call GetSession after available.clear in pool.Dispose()
                pool.Dispose();
                pool.NumberOfAvailableSessions.Should().Be(0);
                pool.NumberOfInUseSessions.Should().Be(0);
                var exception = Record.Exception(() => pool.GetSession());

                pool.NumberOfAvailableSessions.Should().Be(0);
                pool.NumberOfInUseSessions.Should().Be(0);
                exception.Should().BeOfType<InvalidOperationException>();
                exception.Message.Should().Contain("the SessionPool is already started to dispose");
            }
        }

        public class ReleaseMethod
        {
            [Fact]
            public void ShouldReturnToPoolWhenSessionIsHealthyAndPoolIsNotFull()
            {
                var mock = new Mock<IPooledSession>();
                mock.Setup(x => x.IsHealthy).Returns(true);
                var id = new Guid();

                var inUseSessions = new Dictionary<Guid, IPooledSession>();
                inUseSessions.Add(id, mock.Object);
                var pool = new SessionPool(null, inUseSessions);

                pool.NumberOfAvailableSessions.Should().Be(0);
                pool.NumberOfInUseSessions.Should().Be(1);

                pool.Release(id);

                pool.NumberOfAvailableSessions.Should().Be(1);
                pool.NumberOfInUseSessions.Should().Be(0);
            }

            [Fact]
            public void ShouldCloseSessionWhenSessionIsUnhealthy()
            {
                var mock = new Mock<IPooledSession>();
                mock.Setup(x => x.IsHealthy).Returns(false);
                var id = new Guid();

                var inUseSessions = new Dictionary<Guid, IPooledSession>();
                inUseSessions.Add(id, mock.Object);
                var pool = new SessionPool(null, inUseSessions);

                pool.NumberOfAvailableSessions.Should().Be(0);
                pool.NumberOfInUseSessions.Should().Be(1);

                pool.Release(id);

                pool.NumberOfAvailableSessions.Should().Be(0);
                pool.NumberOfInUseSessions.Should().Be(0);
                mock.Verify(x => x.Close(), Times.Once);
            }

            [Fact]
            public void ShouldCloseTheConnectionIfSessionIsHealthyButThePoolIsFull()
            {
                var mock = new Mock<IPooledSession>();
                mock.Setup(x => x.IsHealthy).Returns(true);
                var id = new Guid();

                var inUseSessions = new Dictionary<Guid, IPooledSession>();
                inUseSessions.Add(id, mock.Object);

                var availableSessions = new Queue<IPooledSession>();
                var pooledSessionMock = new Mock<IPooledSession>();
                for (int i = 0; i < Config.DefaultConfig.MaxIdleSessionPoolSize; i++)
                {
                    availableSessions.Enqueue(pooledSessionMock.Object);
                }

                var pool = new SessionPool(availableSessions, inUseSessions);

                pool.NumberOfAvailableSessions.Should().Be(10);
                pool.NumberOfInUseSessions.Should().Be(1);

                pool.Release(id);

                pool.NumberOfAvailableSessions.Should().Be(10);
                pool.NumberOfInUseSessions.Should().Be(0);
                mock.Verify(x => x.Close(), Times.Once);
            }

            // thread safe test
            // Concurrent call of Release and Dispose
            [Fact]
            public void ShouldCloseTheSessionIfPoolDisposeStarted()
            {
                var mock = new Mock<IPooledSession>();
                mock.Setup(x => x.IsHealthy).Returns(true);
                var id = new Guid();

                var inUseSessions = new Dictionary<Guid, IPooledSession>();

                var pool = new SessionPool(null, inUseSessions);

                pool.NumberOfAvailableSessions.Should().Be(0);
                pool.NumberOfInUseSessions.Should().Be(0);

                pool.Dispose();
                inUseSessions.Add(id, mock.Object);
                pool.NumberOfInUseSessions.Should().Be(1);
                pool.Release(id);

                pool.NumberOfAvailableSessions.Should().Be(0);
                pool.NumberOfInUseSessions.Should().Be(0);
                mock.Verify(x => x.Close(), Times.Once);
            }
        }

        public class DisposeMethod
        {
            [Fact]
            public void ShouldReleaseAll()
            {
                var mock = new Mock<IPooledSession>();
                mock.Setup(x => x.IsHealthy).Returns(true);
                var id = Guid.NewGuid();
                var inUseSessions = new Dictionary<Guid, IPooledSession>();
                inUseSessions.Add(id, mock.Object);

                var sessions = new Queue<IPooledSession>();
                var mock1 = new Mock<IPooledSession>();
                mock1.Setup(x => x.IsHealthy).Returns(true);

                sessions.Enqueue(mock1.Object);

                var pool = new SessionPool(sessions, inUseSessions);
                pool.NumberOfAvailableSessions.Should().Be(1);
                pool.NumberOfInUseSessions.Should().Be(1);

                pool.Dispose();

                pool.NumberOfAvailableSessions.Should().Be(0);
                pool.NumberOfInUseSessions.Should().Be(0);
            }

            [Fact]
            public void ShouldLogInUseAndAvailableSessionIds()
            {
                var mockLogger = new Mock<ILogger>();
                var mock = new Mock<IPooledSession>();
                mock.Setup(x => x.IsHealthy).Returns(true);
                var id = Guid.NewGuid();
                var inUseSessions = new Dictionary<Guid, IPooledSession>();
                inUseSessions.Add(id, mock.Object);

                var sessions = new Queue<IPooledSession>();
                var mock1 = new Mock<IPooledSession>();
                mock1.Setup(x => x.IsHealthy).Returns(true);

                sessions.Enqueue(mock1.Object);

                var pool = new SessionPool(sessions, inUseSessions, logger: mockLogger.Object);

                pool.Dispose();

                mockLogger.Verify(x => x.Info(It.Is<string>(actual => actual.StartsWith("Disposing In Use"))),
                    Times.Once);
                mockLogger.Verify(x => x.Info(It.Is<string>(actual => actual.StartsWith("Disposing Available"))),
                    Times.Once);
            }
        }
    }
}