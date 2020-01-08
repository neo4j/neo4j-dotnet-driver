// Copyright (c) 2002-2020 "Neo4j,"
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
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Connector;
using Xunit;

namespace Neo4j.Driver.Tests.Connector
{
    public class DelegatedConnectionTests
    {
        internal class TestDelegatedConnection : DelegatedConnection
        {
            public IList<Exception> ErrorList { get; } = new List<Exception>();

            public TestDelegatedConnection(IConnection connection) : base(connection)
            {
            }

            public override Task OnErrorAsync(Exception error)
            {
                ErrorList.Add(error);
                return Task.FromException(error);
            }
        }

        public class TaskWithErrorHandlingMethod
        {
            private static Task FaultedTask()
            {
                // as it is marked with async, therefore the result will be wrapped in task
                return Task.FromException(new InvalidOperationException("Molly ate too much today!"));
            }

            private static Task FaultedOutsideTask()
            {
                // Though this method return is a task, but this error throws before return a task
                // Therefore there is no task created in this method
                throw new InvalidOperationException("Molly ate too much today!");
            }

            [Fact]
            public async void ShouldHandleBaseErrorForFaultedTask()
            {
                var connMock = new Mock<IConnection>();
                var conn = new TestDelegatedConnection(connMock.Object);

                await Record.ExceptionAsync(() => conn.TaskWithErrorHandling(FaultedTask));

                conn.ErrorList.Count.Should().Be(1);
                conn.ErrorList[0].Should().BeOfType<InvalidOperationException>();
                conn.ErrorList[0].Message.Should().Be("Molly ate too much today!");
            }

            [Fact]
            public async void ShouldHandleBaseErrorForFaultedOutsideTask()
            {
                var connMock = new Mock<IConnection>();
                var conn = new TestDelegatedConnection(connMock.Object);

                FaultedTask().IsFaulted.Should().BeTrue();
                await Record.ExceptionAsync(() => conn.TaskWithErrorHandling(FaultedOutsideTask));

                conn.ErrorList.Count.Should().Be(1);
                conn.ErrorList[0].Should().BeOfType<InvalidOperationException>();
                conn.ErrorList[0].Message.Should().Be("Molly ate too much today!");
            }
        }

        public class ModeProperty
        {
            [Theory]
            [InlineData(AccessMode.Read)]
            [InlineData(AccessMode.Write)]
            public void ShouldGetModeFromDelegate(AccessMode mode)
            {
                var connMock = new Mock<IConnection>();
                connMock.Setup(x => x.Mode).Returns(mode);

                var delegateConnection = new TestDelegatedConnection(connMock.Object);

                delegateConnection.Mode.Should().Be(mode);

                connMock.VerifyGet(x => x.Mode);
            }

            [Theory]
            [InlineData(AccessMode.Read)]
            [InlineData(AccessMode.Write)]
            public void ShouldSetModeOnDelegate(AccessMode mode)
            {
                var connMock = new Mock<IConnection>();
                connMock.Setup(x => x.Mode).Returns(mode);

                var delegateConnection = new TestDelegatedConnection(connMock.Object);

                delegateConnection.Mode = mode;

                connMock.VerifySet(c => c.Mode = mode);
            }
        }
    }
}