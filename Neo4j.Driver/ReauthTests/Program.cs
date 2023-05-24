using Neo4j.Driver;
using Neo4j.Driver.Preview;
using Neo4j.Driver.Auth;

var driver = GraphDatabasePreview.Driver(
    "neo4j://localhost:7687", AuthTokenManagers.Static(AuthTokens.Basic("user1", "password1")));

using(var session1 = driver.AsyncSession())
{
    // do some work as user1
}

using(var session2 = driver.AsyncSession(cfg => cfg.WithAuthToken(AuthTokens.Basic("user2", "password2"))))
{
    // do some work as user2
}
