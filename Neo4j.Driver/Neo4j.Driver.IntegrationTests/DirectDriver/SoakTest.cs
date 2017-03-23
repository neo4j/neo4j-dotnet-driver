using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.Internal;
using Neo4j.Driver.V1;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests
{
    public class SoakTest : DirectDriverIT
    {
        public SoakTest(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture) : base(output, fixture)
        {
        }

        [RequireServerTheory]
        [InlineData(5000)]
//        [InlineData(50000)] leave this to a long dedicated build
        public void SoakRun(int threadCount)
        {
            var statisticsCollector = new StatisticsCollector();
            var driver = GraphDatabase.Driver(ServerEndPoint, AuthTokens.Basic("neo4j", "neo4j"), new Config
                {
                    DriverStatisticsCollector = statisticsCollector,
                    ConnectionTimeout = TimeSpan.FromMilliseconds(-1),
                    EncryptionLevel = EncryptionLevel.Encrypted
                });

            Output.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Started");

            Parallel.For(0, threadCount, i =>
            {
                if (i % 1000 == 0)
                {
                    Output.WriteLine(statisticsCollector.CollectStatistics().ToContentString());
                }

                string[] queries = { "RETURN 1295 + 42", "UNWIND range(1,10000) AS x CREATE (n {prop:x}) DELETE n RETURN sum(x)" };
                try
                {
                    using (var session = driver.Session())
                    {
                        session.Run(queries[i % 2]).Consume();
                    }
                }
                catch (Exception e)
                {
                    Output.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Thread {i} failed to run query {queries[i%2]} due to {e.Message}");
                }
            });

            var st = ConnectionPoolStatistics.Read(statisticsCollector.CollectStatistics());
            Output.WriteLine(st.ReportStatistics().ToContentString());
            Output.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss.ffffff")}] Finished");

            st.ConnToCreate.Should().Be(st.ConnCreated + st.ConnFailedToCreate);
            st.ConnToCreate.Should().Be(st.InUseConns + st.AvailableConns + st.ConnToClose);
            st.ConnClosed.Should().Be(st.ConnClosed);
            st.ConnToCreate.Should().Be(st.ConnCreated);

            driver.Dispose();
        }
    }
}