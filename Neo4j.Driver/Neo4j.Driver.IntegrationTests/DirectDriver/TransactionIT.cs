using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FluentAssertions;
using Neo4j.Driver.V1;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests
{
    public class TransactionIT : DirectDriverIT
    {
        private IDriver Driver => Server.Driver;

        public TransactionIT(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture) : base(output, fixture)
        {
        }

        [Fact]
        public void ShouldRetry()
        {
            using (var session = Driver.Session())
            {
                var timer = new Stopwatch();
                timer.Start();
                var e = Record.Exception(() => session.WriteTransaction(tx =>
                {
                    throw new SessionExpiredException($"Failed at {timer.Elapsed}");
                }));
                timer.Stop();

                var error = e as AggregateException;
                var innerErrors = error.Flatten().InnerExceptions;
                foreach (var innerError in innerErrors)
                {
                    Output.WriteLine(innerError.Message);
                }
                innerErrors.Count.Should().BeGreaterOrEqualTo(5);
                timer.Elapsed.TotalSeconds.Should().BeGreaterOrEqualTo(30);
            }
        }

        [Fact]
        public void ShouldCommitTransactionByDefault()
        {
            using (var session = Driver.Session())
            {
                var createResult = session.WriteTransaction(tx =>
                {
                    var result = tx.Run("CREATE (n) RETURN count(n)");
                    return result.Single()[0].ValueAs<int>();
                });

                // the read operation should see the commited write tx
                var matchResult = session.ReadTransaction(tx =>
                {
                    var result = tx.Run("MATCH (n) RETURN count(n)");
                    return result.Single()[0].ValueAs<int>();
                });

                createResult.Should().Be(matchResult);
            }
        }

        [Fact]
        public void ShouldNotCommitTransaction()
        {
            using (var session = Driver.Session())
            {
                var createResult = session.WriteTransaction(tx =>
                {
                    var result = tx.Run("CREATE (n) RETURN count(n)");
                    var created = result.Single()[0].ValueAs<int>();
                    tx.Failure();
                    return created;
                });

                // the read operation should not see the commited write tx
                var matchResult = session.ReadTransaction(tx =>
                {
                    var result = tx.Run("MATCH (n) RETURN count(n)");
                    return result.Single()[0].ValueAs<int>();
                });

                createResult.Should().Be(matchResult + 1);
            }
        }

        [Fact]
        public void ShouldNotCommitIfError()
        {
            using (var session = Driver.Session())
            {
                Record.Exception(()=>session.WriteTransaction(tx =>
                {
                    tx.Run("CREATE (n) RETURN count(n)");
                    tx.Success();
                    throw new ProtocolException("Broken");
                })).Should().NotBeNull();

                // the read operation should not see the commited write tx
                var matchResult = session.ReadTransaction(tx =>
                {
                    var result = tx.Run("MATCH (n) RETURN count(n)");
                    return result.Single()[0].ValueAs<int>();
                });
                matchResult.Should().Be(0);
            }
        }
    }
}