using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.IntegrationTests.Internals;
using Neo4j.Driver.Internal;
using Neo4j.Driver.V1;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests
{
    public class SessionResetIT : DirectDriverIT
    {
        private IDriver Driver => Server.Driver;

        public SessionResetIT(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
            : base(output, fixture)
        {
            Server.RestartServerWithProcedures(new DirectoryInfo("../../Resources/longRunningStatement.jar").FullName);
        }

        [Fact]
        public async void ShouldKillLongRunningStatement()
        {
            using (var session = Driver.Session())
            {
                var cancelTokenSource = new CancellationTokenSource();
                var resetSession = ResetSessionAfterTimeout(session, 5, cancelTokenSource.Token);

                var result = session.Run("CALL test.driver.longRunningStatement({seconds})",
                    new Dictionary<string, object> { { "seconds", 20 } });
                var exception = Record.Exception(() => result.Consume());

                // if we finished procedure then we cancel the reset timeout
                cancelTokenSource.Cancel();
                await resetSession;

                var serverInfo = result.Summary.Server;

                if (ServerVersion.Version(serverInfo.Version) >= ServerVersion.V3_1_0)
                {
                    exception.Should().BeOfType<TransientException>();
                }
                else
                {
                    exception.Should().BeOfType<ClientException>();
                }
                exception.Message.StartsWith("Failed to invoke procedure `test.driver.longRunningStatement`: " +
                                             "Caused by: org.neo4j.graphdb.TransactionTerminatedException");
            }
        }

        [Fact]
        public async void ShouldKillLongStreamingResult()
        {
            using (var session = Driver.Session())
            {
                var cancelTokenSource = new CancellationTokenSource();
                var resetSession = ResetSessionAfterTimeout(session, 5, cancelTokenSource.Token);

                var result = session.Run("CALL test.driver.longStreamingResult({seconds})",
                    new Dictionary<string, object> { { "seconds", 20L } });

                var exception = Record.Exception(() => result.Consume());

                // if we finished procedure then we cancel the reset timeout
                cancelTokenSource.Cancel();
                await resetSession;

                exception.Should().BeOfType<ClientException>();
                exception.Message.StartsWith(
                    "Failed to call procedure `test.driver.longStreamingResult(seconds :: INTEGER?) :: (record :: STRING?)");
            }
        }

        public async Task ResetSessionAfterTimeout(ISession session, int seconds, CancellationToken cancelToken)
        {
            await Task.Delay(seconds * 1000, cancelToken);
            if (cancelToken.IsCancellationRequested)
            {
                cancelToken.IsCancellationRequested.Should().Be(false);
            }
            else
            {
                session.Reset();
            }
        }

        [Fact]
        public void ShouldAllowMoreStatementAfterSessionReset()
        {
            using (var session = Driver.Session())
            {
                session.Run("RETURN 1").Consume();
                session.Reset();
                session.Run("RETURN 2").Consume();
            }
        }

        [Fact]
        public void ShouldAllowMoreTxAfterSessionReset()
        {
            using (var session = Driver.Session())
            {
                using (var tx = session.BeginTransaction())
                {
                    tx.Run("Return 1");
                    tx.Success();
                }
                session.Reset();
                using (var tx = session.BeginTransaction())
                {
                    tx.Run("RETURN 2");
                    tx.Success();
                }
            }
        }

        [Fact]
        public void ShouldAllowNewTxRunAfterSessionReset()
        {
            using (var session = Driver.Session())
            {
                using (var tx = session.BeginTransaction())
                {
                    session.Reset();
                }
                using (var tx = session.BeginTransaction())
                {
                    tx.Run("RETURN 2");
                    tx.Success();
                }
            }
        }

        [Fact]
        public void ShouldMarkTxAsFailedAndDisallowRunAfterSessionReset()
        {
            using (var session = Driver.Session())
            {
                using (var tx = session.BeginTransaction())
                {
                    session.Reset();
                    var exception = Record.Exception(() => tx.Run("Return 1"));
                    exception.Should().BeOfType<ClientException>();
                    exception.Message.Should().StartWith("Cannot run more statements in this transaction");
                }
            }
        }


        [Fact]
        public void ShouldAllowBeginNewTxAfterResetAndResultConsumed()
        {
            using (var session = Driver.Session())
            {
                var tx1 = session.BeginTransaction();
                var result = tx1.Run("Return 1");
                session.Reset();
                try
                {
                    result.Consume();
                }
                catch
                {
                    // ignored
                }

                using (var tx = session.BeginTransaction())
                {
                    tx.Run("RETURN 2");
                    tx.Success();
                }
            }
        }

        [Fact]
        public async void ShouldThrowExceptionIfErrorAfterResetButNotConsumed()
        {
            using (var session = Driver.Session())
            {
                session.Run("CALL test.driver.longRunningStatement({seconds})",
                    new Dictionary<string, object> { { "seconds", 20 } });
                await Task.Delay(5 * 1000);
                session.Reset();

                var exception = Record.Exception(() => session.BeginTransaction());

                exception.Should().BeOfType<ClientException>();
                exception.Message.Should().StartWith("An error has occurred due to the cancellation of executing a previous statement.");
            }
        }
    }

    internal static class SessionExtension
    {
        public static void Reset(this ISession session)
        {
            var sessionWithReset = (Session) session;
            sessionWithReset.Reset();
        }
    }
}
