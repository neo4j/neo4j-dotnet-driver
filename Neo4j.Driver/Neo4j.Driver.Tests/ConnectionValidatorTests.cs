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
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Xunit;

namespace Neo4j.Driver.Tests
{
    public class ConnectionValidatorTests
    {
        private static ConnectionValidator NewConnectionValidator(
            TimeSpan? connIdleTimeout = null,
            TimeSpan? maxConnLifetime = null)
        {
            if (connIdleTimeout == null)
            {
                connIdleTimeout = Config.InfiniteInterval;
            }

            if (maxConnLifetime == null)
            {
                maxConnLifetime = Config.InfiniteInterval;
            }

            return new ConnectionValidator(connIdleTimeout.Value, maxConnLifetime.Value);
        }

        public class IsConnectionReusableTests
        {
            [Fact]
            public async Task ShouldReturnFalseIfTheConnectionIsNotOpen()
            {
                var conn = new Mock<IPooledConnection>();
                conn.Setup(x => x.IsOpen).Returns(false);
                var validator = NewConnectionValidator();
                var result = await validator.OnReleaseAsync(conn.Object);
                result.Should().BeFalse();
            }

            [Fact]
            public async Task ShouldReturnFalseIfFailedToCleanConnection()
            {
                var conn = new Mock<IPooledConnection>();
                conn.Setup(x => x.IsOpen).Returns(true);
                conn.Setup(x => x.ClearConnectionAsync())
                    .Returns(Task.FromException(new InvalidOperationException()));

                var validator = NewConnectionValidator();
                var result = await validator.OnReleaseAsync(conn.Object);
                result.Should().BeFalse();
            }

            [Fact]
            public async Task ShouldResetIdleTimmer()
            {
                var conn = new Mock<IPooledConnection>();
                var idleTimer = new Mock<ITimer>();
                conn.Setup(x => x.IsOpen).Returns(true);
                conn.Setup(x => x.IdleTimer).Returns(idleTimer.Object);

                var validator = NewConnectionValidator(TimeSpan.Zero);

                var valid = await validator.OnReleaseAsync(conn.Object);
                valid.Should().BeTrue();
                idleTimer.Verify(x => x.Start(), Times.Once);
            }
        }

        public class IsValidMethod
        {
            [Fact]
            public void ShouldBeInvalidIfConnectionIsNotOpen()
            {
                var conn = new Mock<IPooledConnection>();
                conn.Setup(x => x.IsOpen).Returns(false);
                var validator = NewConnectionValidator();
                validator.OnRequire(conn.Object).Should().BeFalse();
            }

            [Fact]
            public void ShouldBeInvalidIfHasBeenIdleForTooLong()
            {
                var conn = new Mock<IPooledConnection>();
                conn.Setup(x => x.IsOpen).Returns(true);
                conn.Setup(x => x.IdleTimer).Returns(MockTimer(10));

                var validator = NewConnectionValidator(TimeSpan.Zero);
                validator.OnRequire(conn.Object).Should().BeFalse();
            }

            [Fact]
            public void ShouldBeInvalidIfHasBeenAliveForTooLong()
            {
                var conn = new Mock<IPooledConnection>();
                conn.Setup(x => x.IsOpen).Returns(true);
                conn.Setup(x => x.IdleTimer).Returns(MockTimer(10));
                conn.Setup(x => x.LifetimeTimer).Returns(MockTimer(10));

                var validator = NewConnectionValidator(maxConnLifetime: TimeSpan.Zero);
                validator.OnRequire(conn.Object).Should().BeFalse();
            }

            [Fact]
            public void ShouldBeValidAndResetIdleTimer()
            {
                var conn = new Mock<IPooledConnection>();
                var idleTimmer = new Mock<ITimer>();
                idleTimmer.Setup(x => x.ElapsedMilliseconds).Returns(10);
                conn.Setup(x => x.IsOpen).Returns(true);
                conn.Setup(x => x.IdleTimer).Returns(idleTimmer.Object);
                conn.Setup(x => x.LifetimeTimer).Returns(MockTimer(10));

                var validator = NewConnectionValidator(TimeSpan.MaxValue, TimeSpan.MaxValue);
                validator.OnRequire(conn.Object).Should().BeTrue();
                idleTimmer.Verify(x => x.Reset(), Times.Once);
            }

            private static ITimer MockTimer(long elapsedMilliseconds)
            {
                var timmer = new Mock<ITimer>();
                timmer.Setup(x => x.ElapsedMilliseconds).Returns(elapsedMilliseconds);
                return timmer.Object;
            }
        }
    }
}
