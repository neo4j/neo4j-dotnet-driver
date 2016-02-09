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
using System.Collections.Concurrent;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Xunit;

namespace Neo4j.Driver.Tests
{
    public class SessionPoolTests
    {
        private static Uri TestUri => new Uri("bolt://localhost");

        public class GetSessionMethod
        {
            [Fact]
            public void ShouldCreateNewSessionWhenQueueIsEmpty()
            {
                var mock = new Mock<IConnection>();
                var pool = new SessionPool(null, TestUri, null, mock.Object);
                pool.GetSession();
                pool.NumberOfAvailableSessions.Should().Be(0);
                pool.NumberOfInUseSessions.Should().Be(1);
            }

            [Fact]
            public void ShouldCreateNewSessionWhenQueueOnlyContainsUnhealthySessions()
            {
                var mock = new Mock<IConnection>();
                var sessions = new ConcurrentQueue<IPooledSession>();
                Guid unhealthyId = Guid.NewGuid();
                var unhealthyMock = new Mock<IPooledSession>();
                unhealthyMock.Setup(x => x.IsHealthy()).Returns(false);
                unhealthyMock.Setup(x => x.Id).Returns(unhealthyId);

                sessions.Enqueue(unhealthyMock.Object);
                var pool = new SessionPool(sessions, TestUri,mock.Object );

                pool.NumberOfAvailableSessions.Should().Be(1);
                pool.NumberOfInUseSessions.Should().Be(0);

                var session = pool.GetSession();

                pool.NumberOfAvailableSessions.Should().Be(0);
                pool.NumberOfInUseSessions.Should().Be(1);
                unhealthyMock.Verify(x => x.Reset(), Times.Never);
                unhealthyMock.Verify(x => x.Close(), Times.Once);

                session.Should().NotBeNull();
                ((IPooledSession)session).Id.Should().NotBe(unhealthyId);
            }

            [Fact]
            public void ShouldReuseOldSessionWhenHealthySessionInQueue()
            {
                var sessions = new ConcurrentQueue<IPooledSession>();
                var mock = new Mock<IPooledSession>();
                mock.Setup(x => x.IsHealthy()).Returns(true);

                sessions.Enqueue(mock.Object);
                var pool = new SessionPool(sessions);

                pool.NumberOfAvailableSessions.Should().Be(1);
                pool.NumberOfInUseSessions.Should().Be(0);

                var session = pool.GetSession();

                pool.NumberOfAvailableSessions.Should().Be(0);
                pool.NumberOfInUseSessions.Should().Be(1);
                mock.Verify(x=>x.Reset(), Times.Once);
                session.Should().Be(mock.Object);
            }


            [Fact]
            public void ShouldReturnFromQueueWhenUsingMultipleThreads()
            {
                var id1 = Guid.NewGuid();
                var id2 = Guid.NewGuid();
                var sessions = new ConcurrentQueue<IPooledSession>();
                var mock1 = new Mock<IPooledSession>();
                mock1.Setup(x => x.IsHealthy()).Returns(true);
                mock1.Setup(x => x.Id).Returns(id1);

                var mock2 = new Mock<IPooledSession>();
                mock2.Setup(x => x.IsHealthy()).Returns(true);
                mock2.Setup(x => x.Id).Returns(id2);

                sessions.Enqueue(mock1.Object);
                sessions.Enqueue(mock2.Object);
                var pool = new SessionPool(sessions);

                pool.NumberOfAvailableSessions.Should().Be(2);
                pool.NumberOfInUseSessions.Should().Be(0);

                Guid t1Id = Guid.Empty, t2Id = Guid.Empty;

                var tasks = new[]
                {
                    Task.Run(() =>
                    {
                        Task.Delay(500);
                        var session = pool.GetSession();
                        t1Id = ((IPooledSession) session).Id;
                    }),
                    Task.Run(() =>
                    {
                        Task.Delay(500);
                        var session = pool.GetSession();
                        t2Id = ((IPooledSession) session).Id;
                    })
                };

                Task.WaitAll(tasks);

                t1Id.Should().NotBe(t2Id);
                var ids = new[] {id1, id2};
                ids.Should().Contain(t1Id);
                ids.Should().Contain(t2Id);

                pool.NumberOfAvailableSessions.Should().Be(0);
                pool.NumberOfInUseSessions.Should().Be(2);
                mock1.Verify(x => x.Reset(), Times.Once);
                mock2.Verify(x => x.Reset(), Times.Once);
            }


            [Fact]
            public void ShouldReuseHealthySessionWhenHealthySessionInQueue()
            {
                var sessions = new ConcurrentQueue<IPooledSession>();
                var healthyMock = new Mock<IPooledSession>();
                healthyMock.Setup(x => x.IsHealthy()).Returns(true);
                var unhealthyMock = new Mock<IPooledSession>();
                unhealthyMock.Setup(x => x.IsHealthy()).Returns(false);

                sessions.Enqueue(unhealthyMock.Object);
                sessions.Enqueue(healthyMock.Object);
                var pool = new SessionPool(sessions);

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

        }

        public class ReleaseMethod
        {
            [Fact]
            public void ShouldReturnToPoolWhenSessionIsHealthy()
            {
                var mock = new Mock<IPooledSession>();
                mock.Setup(x => x.IsHealthy()).Returns(true);
                var id = new Guid();

                var inUseSessions = new ConcurrentDictionary<Guid, IPooledSession>();
                inUseSessions.GetOrAdd(id, mock.Object);
                var pool = new SessionPool(inUseSessions);

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
                mock.Setup(x => x.IsHealthy()).Returns(false);
                var id = new Guid();

                var inUseSessions = new ConcurrentDictionary<Guid, IPooledSession>();
                inUseSessions.GetOrAdd(id, mock.Object);
                var pool = new SessionPool(inUseSessions);

                pool.NumberOfAvailableSessions.Should().Be(0);
                pool.NumberOfInUseSessions.Should().Be(1);

                pool.Release(id);

                pool.NumberOfAvailableSessions.Should().Be(0);
                pool.NumberOfInUseSessions.Should().Be(0);
                mock.Verify(x=>x.Close(), Times.Once);
            }
        }
    }
}
