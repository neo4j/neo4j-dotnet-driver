// Copyright (c) "Neo4j"
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
using System.Net.Sockets;
using FluentAssertions;
using Neo4j.Driver.V1;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests
{
    public class ErrorIT: DirectDriverTestBase
    {
        private IDriver Driver => Server.Driver;

        public ErrorIT(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture) : base(output, fixture)
        {
        }

        [RequireServerFact]
        public void ErrorToRunSessionInTransaction()
        {
            using(var session = Driver.Session())
            using (var tx = session.BeginTransaction())
            {
                var ex = Xunit.Record.Exception(() => session.Run("RETURN 1"));
                ex.Should().BeOfType<ClientException>();
                ex.Message.Should().StartWith("Please close the currently open transaction object");
            }
        }

        [RequireServerFact]
        public void ErrorToRunTransactionInTransaction()
        {
            using(var session = Driver.Session())
            using (var tx = session.BeginTransaction())
            {
                var ex = Xunit.Record.Exception(() => session.BeginTransaction());
                ex.Should().BeOfType<ClientException>();
                ex.Message.Should().StartWith("Please close the currently open transaction object");
            }
        }

        [RequireServerFact]
        public void ErrorToRunInvalidCypher()
        {
            using (var session = Driver.Session())
            {
                var result = session.Run("Invalid Cypher");
                var ex = Xunit.Record.Exception(() => result.Consume());
                ex.Should().BeOfType<ClientException>();
                ex.Message.Should().StartWith("Invalid input");
            }
        }

        [RequireServerFact]
        public void ShouldFailToConnectIncorrectPort()
        {
            using (var driver = GraphDatabase.Driver("bolt://localhost:1234"))
            using (var session = driver.Session())
            {
                var ex = Xunit.Record.Exception(() => session.Run("RETURN 1"));
                ex.Should().BeOfType<ServiceUnavailableException>();
            }
        }

        [RequireServerFact]
        public void ShouldReportWrongScheme()
        {
            var ex = Xunit.Record.Exception(() => GraphDatabase.Driver("http://localhost"));
            ex.Should().BeOfType<NotSupportedException>();
            ex.Message.Should().Be("Unsupported URI scheme: http");
        }
    }
}