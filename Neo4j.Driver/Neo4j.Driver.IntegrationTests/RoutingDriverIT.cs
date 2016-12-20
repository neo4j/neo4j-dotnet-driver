using System;
using System.Linq;
using System.Threading;
using FluentAssertions;
using Neo4j.Driver.V1;
using Xunit;

namespace Neo4j.Driver.IntegrationTests
{
    // If I have a cluster, then I should be able to do the following tests
    public class RoutingDriverIT
    {
        private string routingServer = "bolt+routing://localhost:20003";
        private string wrongServer = "bolt+routing://localhost:1234";
        private IAuthToken authToken = AuthTokens.Basic("neo4j", "1234");

        [Fact]
        public void ShouldConnectClusterWithRoutingScheme()
        {
            try
            {
                using (var driver = GraphDatabase.Driver(routingServer, authToken))
                using (var session = driver.Session())
                {
                    var result = session.Run("UNWIND range(1,10000) AS x CREATE (n {prop:x}) DELETE n RETURN sum(x)");
                    result.Single()[0].ValueAs<int>().Should().Be(10001*10000/2);
                }
            }
            catch (Exception e)
            {
                true.Should().BeFalse(e.Message);
            }
        }

        [Fact]
        public void ShouldLoadBalanceBetweenServers()
        {
            try
            {
                using (var driver = GraphDatabase.Driver(routingServer, authToken))
                {
                    string addr1, addr2;
                    for (int i = 0; i < 10; i++)
                    {
                        using (var session = driver.Session(AccessMode.Read))
                        {
                            var result = session.Run("RETURN 1");
                            addr1 = result.Summary.Server.Address;
                        }
                        using (var session = driver.Session(AccessMode.Read))
                        {
                            addr2 = session.Run("RETURN 2").Summary.Server.Address;
                        }

                        addr1.Should().NotBe(addr2);
                    }
                }
            }
            catch (Exception e)
            {
                true.Should().BeFalse(e.Message);
            }
        }

        [Fact]
        public void ShouldThrowServiceUnavailableExceptionIfNoServer()
        {
            var driver = GraphDatabase.Driver(wrongServer, authToken);
            var error = Record.Exception(()=>driver.Session());
            error.Should().BeOfType<ServiceUnavailableException>();
            error.Message.Should().Be("Failed to connect to any routing server. Please make sure that the cluster is up and can be accessed by the driver and retry.");
            driver.Dispose();
        }

        [Fact]
        public void ShouldDisallowMoreStatementAfterDriverDispose()
        {
            var driver = GraphDatabase.Driver(routingServer, authToken);
            var session = driver.Session(AccessMode.Write);
            session.Run("RETURN 1").Single()[0].ValueAs<int>().Should().Be(1);

            driver.Dispose();
            var error = Record.Exception(() => session.Run("RETURN 1"));
            error.Should().BeOfType<ClientException>();
            error.Message.Should().Contain("The current session cannot be reused as the underlying connection with the server has been closed");
        }

        [Fact]
        public void ShouldDisallowMoreConnectionsAfterDriverDispose()
        {
            var driver = GraphDatabase.Driver(routingServer, authToken);
            var session = driver.Session(AccessMode.Write);
            session.Run("RETURN 1").Single()[0].ValueAs<int>().Should().Be(1);

            driver.Dispose();
            session.Dispose();

            var error = Record.Exception(() => driver.Session());
            error.Should().BeOfType<ObjectDisposedException>();
            error.Message.Should().Contain("Cannot open a new session on a driver that is already disposed.");
        }

        [Theory]
        [InlineData(10)]
        [InlineData(100)]
//        [InlineData(1000)]
        public void SoakRunTests(int threadCount)
        {
            var driver = GraphDatabase.Driver(routingServer, authToken);
            var random = new Random();
            var job = new Job(driver, random);

            var threads= new Thread[threadCount];
            for (int j = 0; j < threadCount; j++)
            {
                var thread = new Thread(job.Execute);
                Thread.Sleep(random.Next(100)); // sleep for sometime
                threads[j] = thread;
                thread.Start();
            }
            for (int i = 0; i < threadCount; i++)
            {
               threads[i].Join(); // wait for each thread to finish
            }

            driver.Dispose();
        }

        public class Job
        {
            private readonly IDriver _driver;
            private static readonly AccessMode[] Access = {AccessMode.Read, AccessMode.Write};
            private static readonly string[] Queries = { "RETURN 1295 + 42", "UNWIND range(1,10000) AS x CREATE (n {prop:x}) DELETE n RETURN sum(x)" };
            private readonly Random _random;

            public Job(IDriver driver, Random random)
            {
                _driver = driver;
                _random = random;
            }

            public void Execute()
            {
                var i = _random.Next(2);
                ISession session = null;
                try
                {
                    session = _driver.Session(Access[i]);
                    var result = session.Run(Queries[i]);
                    switch (i)
                    {
                        case 0:
                            result.Single()[0].ValueAs<int>().Should().Be(1337);
                            Console.WriteLine($"Finished running query {Queries[i]}");
                            break;
                        case 1:
                            result.Single()[0].ValueAs<int>().Should().Be(10001 * 10000 / 2);
                            Console.WriteLine($"Finished running query {Queries[i]}");
                            break;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to run query {Queries[i]} due to {e.Message}");
                    e.Should().BeOfType<SessionExpiredException>();
                    e.Message.Should().Contain("no longer accepts writes");
                }
                finally
                {
                    session?.Dispose();
                }
            }
        }
    }
}
