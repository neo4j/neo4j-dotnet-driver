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

        [Fact]
        public void ServiceUnavailableErrorWhenFailedToConn()
        {
            Exception exception;
            using (var driver = GraphDatabase.Driver("bolt://localhost:123"))
            using (var session = driver.Session())
            {
                exception = Record.Exception(() => session.Run("RETURN 1"));
            }
            exception.Should().BeOfType<ServiceUnavailableException>();
            exception.Message.Should().Be("Connection with the server breaks due to AggregateException: One or more errors occurred.");
            exception.GetBaseException().Should().BeOfType<SocketException>();
            exception.GetBaseException().Message.Should().Contain("No connection could be made because the target machine actively refused it");
        }

        [Fact]
        public void DisallowNewSessionAfterDriverDispose()
        {
            var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken);
            var session = driver.Session(AccessMode.Write);
            session.Run("RETURN 1").Single()[0].ValueAs<int>().Should().Be(1);

            driver.Dispose();
            session.Dispose();

            var error = Record.Exception(() => driver.Session());
            error.Should().BeOfType<ObjectDisposedException>();
            error.Message.Should().Contain("Cannot open a new session on a driver that is already disposed.");
        }

        [Fact]
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

        [Fact]
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

        [Fact]
        public void TheSessionErrorShouldBeClearedForEachSession()
        {
            using (var session = Driver.Session())
            {
                var ex = Record.Exception(() => session.Run("Invalid Cypher").Consume());
                ex.Should().BeOfType<ClientException>();
                ex.Message.Should().StartWith("Invalid input 'I'");
            }
            using (var session = Driver.Session())
            {
                var result = session.Run("RETURN 1");
                result.Single()[0].ValueAs<int>().Should().Be(1);
            }
        }

        [Fact]
        public void AfterErrorTheFirstSyncShouldAckFailureSoThatNewStatementCouldRun()
        {
            using (var session = Driver.Session())
            {
                var ex = Record.Exception(() => session.Run("Invalid Cypher").Consume());
                ex.Should().BeOfType<ClientException>();
                ex.Message.Should().StartWith("Invalid input 'I'");
                var result = session.Run("RETURN 1");
                result.Single()[0].ValueAs<int>().Should().Be(1);
            }
        }

        [Fact]
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
                    ex.Message.Should().StartWith("Invalid input 'I'");
                }

                // Then can run more afterwards
                var result = session.Run("RETURN 1");
                result.Single()[0].ValueAs<int>().Should().Be(1);
            }
        }

        [Fact]
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
                ex.Message.Should().StartWith("Invalid input 'I'");

                // Then can still run more afterwards
                using (var anotherTx = session.BeginTransaction())
                {
                    var result = anotherTx.Run("MATCH (a {name : 'lizhen'}) RETURN count(a)");
                    result.Single()[0].ValueAs<int>().Should().Be(0);
                }
            }
        }

        [Fact]
        public void ShouldNotThrowExceptionWhenDisposeSessionAfterDriver()
        {
            var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken);

            var session = driver.Session();

            using (var tx = session.BeginTransaction())
            {
                var ex = Record.Exception(() => tx.Run("Invalid Cypher").Consume());
                ex.Should().BeOfType<ClientException>();
                ex.Message.Should().StartWith("Invalid input 'I'");
            }

            var result = session.Run("RETURN 1");
            result.Single()[0].ValueAs<int>().Should().Be(1);

            driver.Dispose();
            session.Dispose();
        }
    }
}
