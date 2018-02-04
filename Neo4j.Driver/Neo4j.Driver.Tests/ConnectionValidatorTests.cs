// Copyright (c) 2002-2018 "Neo Technology,"
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
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.V1;
using Xunit;

namespace Neo4j.Driver.Tests
{
    public class ConnectionValidatorTests
    {
        private static ConnectionValidator NewConnectionValidator(
            TimeSpan? connIdleTimeout = null, TimeSpan? maxConnLifetime = null)
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
            public void ShouldReturnFalseIfTheConnectionIsNotOpen()
            {
                var conn = new Mock<IPooledConnection>();
                conn.Setup(x => x.IsOpen).Returns(false);
                var validator = NewConnectionValidator();
                validator.OnRelease(conn.Object).Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnFalseIfFailedToCleanConnection()
            {
                var conn = new Mock<IPooledConnection>();
                conn.Setup(x => x.IsOpen).Returns(true);
                conn.Setup(x => x.ClearConnection()).Throws(new InvalidOperationException());
                var validator = NewConnectionValidator();
                validator.OnRelease(conn.Object).Should().BeFalse();
            }

            [Fact]
            public void ShouldResetIdleTimmer()
            {
                var conn = new Mock<IPooledConnection>();
                var idleTimmer = new Mock<ITimer>();
                conn.Setup(x => x.IsOpen).Returns(true);
                conn.Setup(x => x.IdleTimer).Returns(idleTimmer.Object);

                var validator = NewConnectionValidator(TimeSpan.Zero);

                validator.OnRelease(conn.Object).Should().BeTrue();
                idleTimmer.Verify(x=>x.Start(), Times.Once);
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
                idleTimmer.Verify(x=>x.Reset(), Times.Once);
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
