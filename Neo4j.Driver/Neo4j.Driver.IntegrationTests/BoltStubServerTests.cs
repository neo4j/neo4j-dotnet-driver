using System;
using System.Linq;
using FluentAssertions;
using Neo4j.Driver.IntegrationTests.Internals;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.IntegrationTests
{
    public class BoltStubServerTests
    {
        [RequireBoltStubServerFact]
        public void SendRoutingContextToServer()
        {
            using (BoltStubServer.Start("get_routing_table_with_context", 9001))
            {
                var uri = new Uri("bolt+routing://127.0.0.1:9001/?policy=my_policy&region=china");
                using (var driver = GraphDatabase.Driver(uri, BoltStubServer.Config))
                using (var session = driver.Session())
                {
                    var records = session.Run("MATCH (n) RETURN n.name AS name").ToList();
                    records.Count.Should().Be(2);
                    records[0]["name"].ValueAs<string>().Should().Be("Alice");
                    records[1]["name"].ValueAs<string>().Should().Be("Bob");
                }
            }
        }

        [RequireBoltStubServerFact]
        public void InvokeProcedureGetRoutingTableWhenServerVersionPermits()
        {
            using (BoltStubServer.Start("get_routing_table", 9001))
            {
                var uri = new Uri("bolt+routing://127.0.0.1:9001");
                using (var driver = GraphDatabase.Driver(uri, BoltStubServer.Config))
                using (var session = driver.Session())
                {
                    var records = session.Run("MATCH (n) RETURN n.name AS name").ToList();
                    records.Count.Should().Be(3);
                    records[0]["name"].ValueAs<string>().Should().Be("Alice");
                    records[1]["name"].ValueAs<string>().Should().Be("Bob");
                    records[2]["name"].ValueAs<string>().Should().Be("Eve");
                }
            }
        }

        [RequireBoltStubServerFact]
        public void CanSendMultipleBookmarks()
        {
            var bookmarks = new[]
            {
                "neo4j:bookmark:v1:tx5", "neo4j:bookmark:v1:tx29",
                "neo4j:bookmark:v1:tx94", "neo4j:bookmark:v1:tx56",
                "neo4j:bookmark:v1:tx16", "neo4j:bookmark:v1:tx68"
            };
            using (BoltStubServer.Start("multiple_bookmarks", 9001))
            {
                var uri = new Uri("bolt://127.0.0.1:9001");
                using (var driver = GraphDatabase.Driver(uri, BoltStubServer.Config))
                using (var session = driver.Session(bookmarks))
                {
                    using (var tx = session.BeginTransaction())
                    {
                        tx.Run("CREATE (n {name:'Bob'})");
                        tx.Success();
                    }
                    session.LastBookmark.Should().Be("neo4j:bookmark:v1:tx95");
                }
            }
        }
    }
}
