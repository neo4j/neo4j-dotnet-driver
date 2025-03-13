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
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Protocol;
using Xunit;

namespace Neo4j.Driver.Tests;

public class ConnectionValidatorTests
{
    private static ConnectionValidator NewConnectionValidator(
        TimeSpan? connIdleTimeout = null,
        TimeSpan? maxConnLifetime = null,
        TimeSpan? livelinessCheckTimeout = null)
    {
        return new ConnectionValidator(
            connIdleTimeout ?? Config.InfiniteInterval,
            maxConnLifetime ?? Config.InfiniteInterval,
            livelinessCheckTimeout);
    }

    private static (Mock<IPooledConnection> conn, Mock<ITimer> idle, Mock<ITimer> life) Mock()
    {
        var conn = new Mock<IPooledConnection>();
        conn.Setup(x => x.Version).Returns(BoltProtocolVersion.V5_1);

        var idleTimer = new Mock<ITimer>();
        var lifeTimer = new Mock<ITimer>();

        conn.Setup(x => x.IdleTimer).Returns(idleTimer.Object);
        conn.Setup(x => x.LifetimeTimer).Returns(lifeTimer.Object);
        return (conn, idleTimer, lifeTimer);
    }

    public class IsConnectionReusableTests
    {
        [Fact]
        public async Task ShouldReturnFalseIfTheConnectionIsNotOpen()
        {
            var (conn, _, _) = Mock();
            conn.Setup(x => x.IsOpen).Returns(false);
            var validator = NewConnectionValidator();
            var result = await validator.OnReleaseAsync(conn.Object);
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ShouldReturnFalseIfFailedToCleanConnection()
        {
            var (conn, _, _) = Mock();
            conn.Setup(x => x.IsOpen).Returns(true);
            conn.Setup(x => x.ClearConnectionAsync())
                .Returns(Task.FromException(new InvalidOperationException()));

            var validator = NewConnectionValidator();
            var result = await validator.OnReleaseAsync(conn.Object);
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ShouldResetIdleTimer()
        {
            var (conn, idleTimer, _) = Mock();
            conn.Setup(x => x.IsOpen).Returns(true);

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
            var (conn, _, _) = Mock();
            conn.Setup(x => x.IsOpen).Returns(false);
            var validator = NewConnectionValidator();
            validator.GetConnectionLifetimeStatus(conn.Object).Should().Be(AcquireStatus.Unhealthy);
        }

        [Fact]
        public void ShouldBeInvalidIfHasBeenIdleForTooLong()
        {
            var (conn, idleTimer, _) = Mock();
            conn.Setup(x => x.IsOpen).Returns(true);
            idleTimer.Setup(x => x.ElapsedMilliseconds).Returns(10);

            var validator = NewConnectionValidator(TimeSpan.Zero);
            validator.GetConnectionLifetimeStatus(conn.Object).Should().Be(AcquireStatus.Unhealthy);
        }

        [Fact]
        public void ShouldBeInvalidIfHasBeenAliveForTooLong()
        {
            var (conn, idleTimer, lifeTimer) = Mock();
            conn.Setup(x => x.IsOpen).Returns(true);
            idleTimer.Setup(x => x.ElapsedMilliseconds).Returns(10);
            lifeTimer.Setup(x => x.ElapsedMilliseconds).Returns(10);

            var validator = NewConnectionValidator(maxConnLifetime: TimeSpan.Zero);
            validator.GetConnectionLifetimeStatus(conn.Object).Should().Be(AcquireStatus.Unhealthy);
        }

        [Fact]
        public void ShouldBeValidAndResetIdleTimer()
        {
            var (conn, idleTimer, _) = Mock();
            idleTimer.Setup(x => x.ElapsedMilliseconds).Returns(10);
            conn.Setup(x => x.IsOpen).Returns(true);

            var validator = NewConnectionValidator(TimeSpan.MaxValue, TimeSpan.MaxValue);
            validator.GetConnectionLifetimeStatus(conn.Object).Should().Be(AcquireStatus.Healthy);
            idleTimer.Verify(x => x.Reset(), Times.Once);
        }

        [Fact]
        public void ShouldRequireLiveness()
        {
            var (conn, idleTimer, _) = Mock();
            idleTimer.Setup(x => x.ElapsedMilliseconds).Returns(10);
            conn.Setup(x => x.IsOpen).Returns(true);

            var validator = NewConnectionValidator(TimeSpan.MaxValue, TimeSpan.MaxValue, TimeSpan.FromMilliseconds(9));
            validator.GetConnectionLifetimeStatus(conn.Object).Should().Be(AcquireStatus.RequiresLivenessProbe);
            idleTimer.Verify(x => x.Reset(), Times.Once);
        }
    }
}
