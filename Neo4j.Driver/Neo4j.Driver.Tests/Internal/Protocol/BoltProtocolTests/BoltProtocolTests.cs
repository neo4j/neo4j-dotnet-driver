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
using Neo4j.Driver.Internal.Connector;
using Xunit;

namespace Neo4j.Driver.Internal.Protocol.BoltProtocolTests;

public class BoltProtocolTests
{
    public class RunInAutoCommitTransactionAsyncTests
    {
        [Theory]
        [InlineData(4, 3)]
        [InlineData(4, 2)]
        [InlineData(4, 1)]
        [InlineData(4, 0)]
        public async Task ShouldThrowWhenUsingImpersonatedUserWithBoltVersionLessThan44(int major, int minor)
        {
            var mockConn = new Mock<IConnection>();
            mockConn.SetupGet(x => x.Version).Returns(new BoltProtocolVersion(major, minor));

            var acp = new AutoCommitParams
            {
                ImpersonatedUser = "Douglas Fir"
            };

            var exception = await Record.ExceptionAsync(
                () => BoltProtocol.Instance.RunInAutoCommitTransactionAsync(mockConn.Object, acp));

            exception.Should().BeOfType<ArgumentException>();
        }

        [Theory]
        [InlineData(4, 4)]
        [InlineData(5, 0)]
        [InlineData(6, 0)]
        public async Task ShouldNotThrowWhenImpersonatingUserWithBoltVersionGreaterThan43(int major, int minor)
        {
            var mockConn = new Mock<IConnection>();
            mockConn.SetupGet(x => x.Version).Returns(new BoltProtocolVersion(major, minor));
            mockConn.SetupGet(x => x.Mode).Returns(AccessMode.Read);

            var acp = new AutoCommitParams
            {
                Query = new Query("..."),
                ImpersonatedUser = "Douglas Fir"
            };

            var exception = await Record.ExceptionAsync(
                () => BoltProtocol.Instance.RunInAutoCommitTransactionAsync(mockConn.Object, acp));

            exception.Should().BeNull();
        }
    }
}
