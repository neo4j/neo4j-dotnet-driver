using System;
using Xunit;

namespace Neo4j.Driver.IntegrationTests
{
    public class ConnectionTest
    {
        [Fact]
        public void ShouldDoHandShake()
        {
            using (var driver = GraphDatabase.Driver("http://localhost:7687"))
            {
                using (var session = driver.Session())
                {
                    Console.WriteLine("lala");
                    //Console.ReadKey();
                }
            }
        }

        [Fact]
        public void DoesNothing()
        {
        }
    }
}