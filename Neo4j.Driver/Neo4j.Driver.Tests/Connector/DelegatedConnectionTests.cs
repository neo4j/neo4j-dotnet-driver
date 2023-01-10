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
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Connector;
using Xunit;

namespace Neo4j.Driver.Tests.Connector
{
    public class DelegatedConnectionTests
    {
        internal class TestDelegatedConnection : DelegatedConnection
        {
            public TestDelegatedConnection(IConnection connection) : base(connection)
            {
            }

            public IList<Exception> ErrorList { get; } = new List<Exception>();

            internal override Task OnErrorAsync(Exception error)
            {
                ErrorList.Add(error);
                return Task.FromException(error);
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
        }
    }
}
