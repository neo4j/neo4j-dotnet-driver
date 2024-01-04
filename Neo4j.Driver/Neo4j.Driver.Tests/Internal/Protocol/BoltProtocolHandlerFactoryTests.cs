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
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Internal.Result;
using Xunit;
using Record = Xunit.Record;

namespace Neo4j.Driver.Tests.Internal.Protocol;

public class BoltProtocolHandlerFactoryTests
{
    public class NewResultCursorBuilderTests
    {
        [Fact]
        public void ShouldAllowNullFunctions()
        {
            var summaryBuilder = new SummaryBuilder(
                new Query("..."),
                new ServerInfo(new Uri("bolt://127.0.0.1:7687")));

            var ex = Record.Exception(
                () =>
                {
                    BoltProtocolHandlerFactory.Instance.NewResultCursorBuilder(
                        summaryBuilder,
                        new Mock<IConnection>().Object,
                        null,
                        null,
                        null,
                        null,
                        -1,
                        true,
                        It.IsAny<IInternalAsyncTransaction>());
                });

            ex.Should().BeNull();
        }
    }
}
