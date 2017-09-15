// Copyright (c) 2002-2017 "Neo Technology,"
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
using Neo4j.Driver.V1;
using Xunit;
using Xunit.Abstractions;
using System.Linq;
using System.Net.Sockets;

namespace Neo4j.Driver.IntegrationTests
{
    public class SessionIT : DirectDriverIT
    {
        private IDriver Driver => Server.Driver;

        public SessionIT(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture) : base(output, fixture)
        {
        }

        [RequireServerFact]
        public void ServiceUnavailableErrorWhenFailedToConn()
        {
            Exception exception;
            using (var driver = GraphDatabase.Driver("bolt://localhost:123"))
            using (var session = driver.Session())
            {
                exception = Record.Exception(() => session.Run("RETURN 1"));
            }
            exception.Should().BeOfType<ServiceUnavailableException>();
            exception.Message.Should().Contain("Connection with the server breaks");
            exception.GetBaseException().Should().BeOfType<SocketException>();
        }

        [RequireServerFact]
        public void DisallowNewSessionAfterDriverDispose()
        {
            var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken);
            var session = driver.Session();
            session.Run("RETURN 1").Single()[0].ValueAs<int>().Should().Be(1);

            driver.Dispose();
            session.Dispose();

            var error = Record.Exception(() => driver.Session());
            error.Should().BeOfType<ObjectDisposedException>();
            error.Message.Should().Contain("Cannot open a new session on a driver that is already disposed.");
        }

        [RequireServerFact]
        public void DisallowRunInSessionAfterDriverDispose()
        {
            var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken);
            var session = driver.Session();
            session.Run("RETURN 1").Single()[0].ValueAs<int>().Should().Be(1);

            driver.Dispose();

            var error = Record.Exception(() => session.Run("RETURN 1"));
            error.Should().BeOfType<ObjectDisposedException>();
            error.Message.Should().StartWith("Failed to acquire a new connection as the driver has already been disposed.");
        }

        [RequireServerFact]
        public void ShouldConnectAndRun()
        {
            using (var session = Driver.Session())
            {
                var result = session.Run("RETURN 2 as Number");
                result.Consume();
                result.Keys.Should().Contain("Number");
                result.Keys.Count.Should().Be(1);
            }
        }

        [RequireServerFact]
        public void ShouldBeAbleToRunMultiStatementsInOneTransaction()
        {
            using (var session = Driver.Session())
            using (var tx = session.BeginTransaction())
            {
                // clean db
                tx.Run("MATCH (n) DETACH DELETE n RETURN count(*)");
                var result = tx.Run("CREATE (n {name:'Steve Brook'}) RETURN n.name");

                var record = result.Single();
                record["n.name"].Should().Be("Steve Brook");
            }
        }

        [RequireServerFact]
        public void TheSessionErrorShouldBeClearedForEachSession()
        {
            using (var session = Driver.Session())
            {
                var ex = Record.Exception(() => session.Run("Invalid Cypher").Consume());
                ex.Should().BeOfType<ClientException>();
                ex.Message.Should().StartWith("Invalid input");
            }
            using (var session = Driver.Session())
            {
                var result = session.Run("RETURN 1");
                result.Single()[0].ValueAs<int>().Should().Be(1);
            }
        }

        [RequireServerFact]
        public void AfterErrorTheFirstSyncShouldAckFailureSoThatNewStatementCouldRun()
        {
            using (var session = Driver.Session())
            {
                var ex = Record.Exception(() => session.Run("Invalid Cypher").Consume());
                ex.Should().BeOfType<ClientException>();
                ex.Message.Should().StartWith("Invalid input");
                var result = session.Run("RETURN 1");
                result.Single()[0].ValueAs<int>().Should().Be(1);
            }
        }

        [RequireServerFact]
        public void RollBackTxIfErrorWithConsume()
        {
            // Given
            using (var session = Driver.Session())
            {
                // When failed to run a tx with consume
                using (var tx = session.BeginTransaction())
                {
                    var ex = Record.Exception(() => tx.Run("Invalid Cypher").Consume());
                    ex.Should().BeOfType<ClientException>();
                    ex.Message.Should().StartWith("Invalid input");
                }

                // Then can run more afterwards
                var result = session.Run("RETURN 1");
                result.Single()[0].ValueAs<int>().Should().Be(1);
            }
        }

        [RequireServerFact]
        public void RollBackTxIfErrorWithoutConsume()
        {
            // Given
            using (var session = Driver.Session())
            {
                // When failed to run a tx without consume

                // The following code is the same as using(var tx = session.BeginTx()) {...}
                // While we have the full control of where the error is thrown
                var tx = session.BeginTransaction();
                tx.Run("CREATE (a { name: 'lizhen' })");
                tx.Run("Invalid Cypher");
                tx.Success();
                var ex = Record.Exception(() => tx.Dispose());
                ex.Should().BeOfType<ClientException>();
                ex.Message.Should().StartWith("Invalid input");

                // Then can still run more afterwards
                using (var anotherTx = session.BeginTransaction())
                {
                    var result = anotherTx.Run("MATCH (a {name : 'lizhen'}) RETURN count(a)");
                    result.Single()[0].ValueAs<int>().Should().Be(0);
                }
            }
        }

        [RequireServerFact]
        public void ShouldNotThrowExceptionWhenDisposeSessionAfterDriver()
        {
            var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken);

            var session = driver.Session();

            using (var tx = session.BeginTransaction())
            {
                var ex = Record.Exception(() => tx.Run("Invalid Cypher").Consume());
                ex.Should().BeOfType<ClientException>();
                ex.Message.Should().StartWith("Invalid input");
            }

            var result = session.Run("RETURN 1");
            result.Single()[0].ValueAs<int>().Should().Be(1);

            driver.Dispose();
            session.Dispose();
        }

        [RequireServerFact]
        public void KeysShouldBeAvailableOnlyAfterRunAndResultConsumption()
        {
            using (var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken))
            {
                using (var session = driver.Session())
                {
                    var result = session.Run("RETURN 1 As X");
                    result.Keys.Should().BeEmpty();
                    result.Consume();
                    result.Keys.Should().HaveCount(1);
                    result.Keys.Should().Contain("X");
                }
            }
        }

        [RequireServerFact]
        public void KeysShouldBeAvailableOnlyAfterConsecutiveRunAndResultConsumption()
        {
            using (var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken))
            {
                using (var session = driver.Session())
                {
                    var result1 = session.Run("RETURN 1 As X");
                    var result2 = session.Run("RETURN 1 As Y");
                    //result1.Keys.Should().BeEmpty(); // Failed
                    result1.Keys.Should().HaveCount(1);
                    result1.Keys.Should().Contain("X");

                    result2.Keys.Should().BeEmpty();
                    result1.Consume();
                    result1.Keys.Should().HaveCount(1);
                    result1.Keys.Should().Contain("X");

                    result2.Keys.Should().BeEmpty();
                    result2.Consume();
                    result2.Keys.Should().HaveCount(1);
                    result2.Keys.Should().Contain("Y");
                }
            }
        }

        [RequireServerFact]
        public async void KeysShouldBeAvailableJustAfterRunAsync()
        {
            using (var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken))
            {
                using (var session = driver.Session())
                {
                    var cursor = await session.RunAsync("RETURN 1 As X");
                    cursor.Keys.Should().HaveCount(1);
                    cursor.Keys.Should().Contain("X");
                }
            }
        }

        [RequireServerFact]
        public async void KeysShouldBeAvailableJustAfterConsecutiveRunAsync()
        {
            using (var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken))
            {
                using (var session = driver.Session())
                {
                    var cursor1 = await session.RunAsync("RETURN 1 As X");
                    var cursor2 = await session.RunAsync("RETURN 1 As Y");

                    cursor1.Keys.Should().HaveCount(1);
                    cursor1.Keys.Should().Contain("X");

                    cursor2.Keys.Should().HaveCount(1);
                    cursor2.Keys.Should().Contain("Y");
                }
            }
        }

        [RequireServerFact]
        public async void KeysShouldBeAvailableJustAfterConsecutiveRunAsyncWithConsumptionInBetween()
        {
            using (var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken))
            {
                using (var session = driver.Session())
                {
                    var cursor1 = await session.RunAsync("RETURN 1 As X");
                    var cursor2 = await session.RunAsync("RETURN 1 As Y");

                    await cursor1.ConsumeAsync();

                    cursor1.Keys.Should().HaveCount(1);
                    cursor1.Keys.Should().Contain("X");

                    cursor2.Keys.Should().HaveCount(1);
                    cursor2.Keys.Should().Contain("Y");
                }
            }
        }

    }
}
