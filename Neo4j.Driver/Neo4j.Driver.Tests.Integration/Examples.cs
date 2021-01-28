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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using FluentAssertions;
//The only imported needed for using this driver
using Neo4j.Driver;
using Neo4j.Driver.IntegrationTests;
using Neo4j.Driver.IntegrationTests.Internals;
using static Neo4j.Driver.IntegrationTests.DatabaseExtensions;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.Examples
{
    /// <summary>
    /// The driver examples since 1.2 driver
    /// </summary>
    public class Examples
    {
        [SuppressMessage("ReSharper", "xUnit1013")]
        public class AutocommitTransactionExample : BaseExample
        {
            public AutocommitTransactionExample(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
                : base(output, fixture)
            {
            }

            // tag::autocommit-transaction[]
            public void AddPerson(string name)
            {
                using (var session = Driver.Session())
                {
                    session.Run("CREATE (a:Person {name: $name})", new {name});
                }
            }
            // end::autocommit-transaction[]

            [RequireServerFact]
            public void TestAutocommitTransactionExample()
            {
                // Given & When
                AddPerson("Alice");
                // Then
                CountPerson("Alice").Should().Be(1);
            }
        }

        public class BasicAuthExample : BaseExample
        {
            public BasicAuthExample(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
                : base(output, fixture)
            {
            }

            // tag::basic-auth[]
            public IDriver CreateDriverWithBasicAuth(string uri, string user, string password)
            {
                return GraphDatabase.Driver(uri, AuthTokens.Basic(user, password));
            }
            // end::basic-auth[]

            [RequireServerFact]
            public void TestBasicAuthExample()
            {
                // Given
                using (var driver = CreateDriverWithBasicAuth(Uri, User, Password))
                using (var session = driver.Session())
                {
                    // When & Then
                    session.Run("RETURN 1").Single()[0].As<int>().Should().Be(1);
                }
            }
        }

        public class ConfigConnectionPoolExample : BaseExample
        {
            public ConfigConnectionPoolExample(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
                : base(output, fixture)
            {
            }

            // tag::config-connection-pool[]
            public IDriver CreateDriverWithCustomizedConnectionPool(string uri, string user, string password)
            {
                return GraphDatabase.Driver(uri, AuthTokens.Basic(user, password),
                    o => o.WithMaxConnectionLifetime(TimeSpan.FromMinutes(30))
                        .WithMaxConnectionPoolSize(50)
                        .WithConnectionAcquisitionTimeout(TimeSpan.FromMinutes(2)));
            }
            // end::config-connection-pool[]

            [RequireServerFact]
            public void TestConfigConnectionPoolExample()
            {
                // Given
                using (var driver = CreateDriverWithCustomizedConnectionPool(Uri, User, Password))
                using (var session = driver.Session())
                {
                    // When & Then
                    session.Run("RETURN 1").Single()[0].As<int>().Should().Be(1);
                }
            }
        }

        public class ConfigConnectionTimeoutExample : BaseExample
        {
            public ConfigConnectionTimeoutExample(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
                : base(output, fixture)
            {
            }

            // tag::config-connection-timeout[]
            public IDriver CreateDriverWithCustomizedConnectionTimeout(string uri, string user, string password)
            {
                return GraphDatabase.Driver(uri, AuthTokens.Basic(user, password),
                    o => o.WithConnectionTimeout(TimeSpan.FromSeconds(15)));
            }
            // end::config-connection-timeout[]

            [RequireServerFact]
            public void TestConfigConnectionTimeoutExample()
            {
                // Given
                using (var driver = CreateDriverWithCustomizedConnectionTimeout(Uri, User, Password))
                using (var session = driver.Session())
                {
                    // When & Then
                    session.Run("RETURN 1").Single()[0].As<int>().Should().Be(1);
                }
            }
        }

        public class ConfigMaxRetryTimeExample : BaseExample
        {
            public ConfigMaxRetryTimeExample(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
                : base(output, fixture)
            {
            }

            // tag::config-max-retry-time[]
            public IDriver CreateDriverWithCustomizedMaxRetryTime(string uri, string user, string password)
            {
                return GraphDatabase.Driver(uri, AuthTokens.Basic(user, password),
                    o => o.WithMaxTransactionRetryTime(TimeSpan.FromSeconds(15)));
            }
            // end::config-max-retry-time[]

            [RequireServerFact]
            public void TestConfigMaxRetryTimeExample()
            {
                // Given
                using (var driver = CreateDriverWithCustomizedMaxRetryTime(Uri, User, Password))
                using (var session = driver.Session())
                {
                    // When & Then
                    session.Run("RETURN 1").Single()[0].As<int>().Should().Be(1);
                }
            }
        }

        public class ConfigTrustExample : BaseExample
        {
            public ConfigTrustExample(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
                : base(output, fixture)
            {
            }

            // tag::config-trust[]
            public IDriver CreateDriverWithCustomizedTrustStrategy(string uri, string user, string password)
            {
                return GraphDatabase.Driver(uri, AuthTokens.Basic(user, password),
                    o => o.WithTrustManager(TrustManager.CreateInsecure()));
            }
            // end::config-trust[]

            [RequireServerFact]
            public void TestConfigTrustExample()
            {
                // Given
                using (var driver = CreateDriverWithCustomizedTrustStrategy(Uri, User, Password))
                using (var session = driver.Session())
                {
                    // When & Then
                    session.Run("RETURN 1").Single()[0].As<int>().Should().Be(1);
                }
            }
        }

        public class ConfigUnencryptedExample : BaseExample
        {
            public ConfigUnencryptedExample(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
                : base(output, fixture)
            {
            }

            // tag::config-unencrypted[]
            public IDriver CreateDriverWithCustomizedSecurityStrategy(string uri, string user, string password)
            {
                return GraphDatabase.Driver(uri, AuthTokens.Basic(user, password),
                    o=>o.WithEncryptionLevel(EncryptionLevel.None));
            }
            // end::config-unencrypted[]

            [RequireServerFact]
            public void TestConfigUnencryptedExample()
            {
                // Given
                using (var driver = CreateDriverWithCustomizedSecurityStrategy(Uri, User, Password))
                using (var session = driver.Session())
                {
                    // When & Then
                    session.Run("RETURN 1").Single()[0].As<int>().Should().Be(1);
                }
            }
        }

        [SuppressMessage("ReSharper", "xUnit1013")]
        public class ConfigCustomResolverExample
        {
            private const string Username = "neo4j";
            private const string Password = "some password";

            // tag::config-custom-resolver[]
            private IDriver CreateDriverWithCustomResolver(string virtualUri, IAuthToken token,
                params ServerAddress[] addresses)
            {
                return GraphDatabase.Driver(virtualUri, token,
                    o => o.WithResolver(new ListAddressResolver(addresses)).WithEncryptionLevel(EncryptionLevel.None));
            }

            public void AddPerson(string name)
            {
                using (var driver = CreateDriverWithCustomResolver("neo4j://x.example.com",
                    AuthTokens.Basic(Username, Password),
                    ServerAddress.From("a.example.com", 7687), ServerAddress.From("b.example.com", 7877),
                    ServerAddress.From("c.example.com", 9092)))
                {
                    using (var session = driver.Session())
                    {
                        session.Run("CREATE (a:Person {name: $name})", new {name});
                    }
                }
            }

            private class ListAddressResolver : IServerAddressResolver
            {
                private readonly ServerAddress[] servers;

                public ListAddressResolver(params ServerAddress[] servers)
                {
                    this.servers = servers;
                }

                public ISet<ServerAddress> Resolve(ServerAddress address)
                {
                    return new HashSet<ServerAddress>(servers);
                }
            }
            // end::config-custom-resolver[]

            [RequireBoltStubServerFactAttribute]
            public void TestCustomResolverExample()
            {
                using (var server1 = BoltStubServer.Start("V4/get_routing_table_only", 9001))
                {
                    using (var server2 = BoltStubServer.Start("V4/return_1", 9002))
                    {
                        using (var driver =
                            CreateDriverWithCustomResolver("neo4j://x.example.com", AuthTokens.None,
                                ServerAddress.From("127.0.0.1", 9001)))
                        {
                            using (var session = driver.Session(o => o.WithDefaultAccessMode(AccessMode.Read)))
                            {
                                // When & Then
                                session.Run("RETURN 1").Single()[0].As<int>().Should().Be(1);
                            }
                        }
                    }
                }
            }
        }

        public class CustomAuthExample : BaseExample
        {
            public CustomAuthExample(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
                : base(output, fixture)
            {
            }

            // tag::custom-auth[]
            public IDriver CreateDriverWithCustomizedAuth(string uri,
                string principal, string credentials, string realm, string scheme,
                Dictionary<string, object> parameters)
            {
                return GraphDatabase.Driver(uri, AuthTokens.Custom(principal, credentials, realm, scheme, parameters),
                    o=>o.WithEncryptionLevel(EncryptionLevel.None));
            }
            // end::custom-auth[]

            [RequireServerFact]
            public void TestCustomAuthExample()
            {
                // Given
                using (var driver = CreateDriverWithCustomizedAuth(Uri, User, Password, "native", "basic", null))
                using (var session = driver.Session())
                {
                    // When & Then
                    session.Run("RETURN 1").Single()[0].As<int>().Should().Be(1);
                }
            }
        }

        public class KerberosAuthExample : BaseExample
        {
            public KerberosAuthExample(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
                : base(output, fixture)
            {
            }

            // tag::kerberos-auth[]
            public IDriver CreateDriverWithKerberosAuth(string uri, string ticket)
            {
                return GraphDatabase.Driver(uri, AuthTokens.Kerberos(ticket),
                    o => o.WithEncryptionLevel(EncryptionLevel.None));
            }
            // end::kerberos-auth[]

            [RequireServerFact]
            public void TestKerberosAuthExample()
            {
                // Given
                using (var driver = CreateDriverWithKerberosAuth(Uri, "kerberos ticket"))
                {
                    // When & Then
                    driver.Should().BeOfType<Internal.Driver>();
                }
            }
        }

        public class CypherErrorExample : BaseExample
        {
            public CypherErrorExample(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
                : base(output, fixture)
            {
            }

            // tag::cypher-error[]
            public int GetEmployeeNumber(string name)
            {
                using (var session = Driver.Session())
                {
                    return session.ReadTransaction(tx => SelectEmployee(tx, name));
                }
            }

            private int SelectEmployee(ITransaction tx, string name)
            {
                try
                {
                    var result = tx.Run("SELECT * FROM Employees WHERE name = $name", new {name});
                    return result.Single()["employee_number"].As<int>();
                }
                catch (ClientException ex)
                {
                    Output.WriteLine(ex.Message);
                    return -1;
                }
            }
            // end::cypher-error[]

            [RequireServerFact]
            public void TestCypherErrorExample()
            {
                // When & Then
                GetEmployeeNumber("Alice").Should().Be(-1);
            }
        }

        public class DriverLifecycleExampleTest : BaseExample
        {
            public DriverLifecycleExampleTest(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
                : base(output, fixture)
            {
            }

            // tag::driver-lifecycle[]
            public class DriverLifecycleExample : IDisposable
            {
                public IDriver Driver { get; }

                public DriverLifecycleExample(string uri, string user, string password)
                {
                    Driver = GraphDatabase.Driver(uri, AuthTokens.Basic(user, password));
                }

                public void Dispose()
                {
                    Driver?.Dispose();
                }
            }
            // end::driver-lifecycle[]

            [RequireServerFact]
            public void TestDriverLifecycleExample()
            {
                // Given
                var driver = new DriverLifecycleExample(Uri, User, Password).Driver;
                using (var session = driver.Session())
                {
                    // When & Then
                    session.Run("RETURN 1").Single()[0].As<int>().Should().Be(1);
                }
            }
        }

        public class HelloWorldExampleTest : BaseExample
        {
            public HelloWorldExampleTest(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
                : base(output, fixture)
            {
            }

            [RequireServerFact]
            public void TestHelloWorldExample()
            {
                // Given
                using (var example = new HelloWorldExample(Uri, User, Password))
                {
                    // When & Then
                    example.PrintGreeting("Hello, world");
                }
            }

            // tag::hello-world[]
            public class HelloWorldExample : IDisposable
            {
                private readonly IDriver _driver;

                public HelloWorldExample(string uri, string user, string password)
                {
                    _driver = GraphDatabase.Driver(uri, AuthTokens.Basic(user, password));
                }

                public void PrintGreeting(string message)
                {
                    using (var session = _driver.Session())
                    {
                        var greeting = session.WriteTransaction(tx =>
                        {
                            var result = tx.Run("CREATE (a:Greeting) " +
                                                "SET a.message = $message " +
                                                "RETURN a.message + ', from node ' + id(a)",
                                new {message});
                            return result.Single()[0].As<string>();
                        });
                        Console.WriteLine(greeting);
                    }
                }

                public void Dispose()
                {
                    _driver?.Dispose();
                }

                public static void Main()
                {
                    using (var greeter = new HelloWorldExample("bolt://localhost:7687", "neo4j", "password"))
                    {
                        greeter.PrintGreeting("hello, world");
                    }
                }
            }
            // end::hello-world[]
        }

        public class ReadWriteTransactionExample : BaseExample
        {
            public ReadWriteTransactionExample(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
                : base(output, fixture)
            {
            }

            [RequireServerFact]
            public void TestReadWriteTransactionExample()
            {
                // When & Then
                AddPerson("Alice").Should().BeGreaterOrEqualTo(0L);
            }

            // tag::read-write-transaction[]
            public long AddPerson(string name)
            {
                using (var session = Driver.Session())
                {
                    session.WriteTransaction(tx => CreatePersonNode(tx, name));
                    return session.ReadTransaction(tx => MatchPersonNode(tx, name));
                }
            }

            private static IResult CreatePersonNode(ITransaction tx, string name)
            {
                return tx.Run("CREATE (a:Person {name: $name})", new {name});
            }

            private static long MatchPersonNode(ITransaction tx, string name)
            {
                var result = tx.Run("MATCH (a:Person {name: $name}) RETURN id(a)", new {name});
                return result.Single()[0].As<long>();
            }

            // end::read-write-transaction[]
        }

        public class ResultConsumeExample : BaseExample
        {
            public ResultConsumeExample(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
                : base(output, fixture)
            {
            }

            // tag::result-consume[]
            public List<string> GetPeople()
            {
                using (var session = Driver.Session())
                {
                    return session.ReadTransaction(tx =>
                    {
                        var result = tx.Run("MATCH (a:Person) RETURN a.name ORDER BY a.name");
                        return result.Select(record => record[0].As<string>()).ToList();
                    });
                }
            }
            // end::result-consume[]

            [RequireServerFact]
            public void TestResultConsumeExample()
            {
                // Given
                Write("CREATE (a:Person {name: 'Alice'})");
                Write("CREATE (a:Person {name: 'Bob'})");
                // When & Then
                GetPeople().Should().Contain(new[] {"Alice", "Bob"});
            }
        }

        public class ResultRetainExample : BaseExample
        {
            public ResultRetainExample(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
                : base(output, fixture)
            {
            }

            // tag::result-retain[]
            public int AddEmployees(string companyName)
            {
                using (var session = Driver.Session())
                {
                    var persons =
                        session.ReadTransaction(tx => tx.Run("MATCH (a:Person) RETURN a.name AS name").ToList());
                    return persons.Sum(person => session.WriteTransaction(tx =>
                    {
                        tx.Run("MATCH (emp:Person {name: $person_name}) " +
                               "MERGE (com:Company {name: $company_name}) " +
                               "MERGE (emp)-[:WORKS_FOR]->(com)",
                            new {person_name = person["name"].As<string>(), company_name = companyName});
                        return 1;
                    }));
                }
            }
            // end::result-retain[]

            [RequireServerFact]
            public void TestResultConsumeExample()
            {
                // Given
                Write("CREATE (a:Person {name: 'Alice'})");
                Write("CREATE (a:Person {name: 'Bob'})");
                // When & Then
                AddEmployees("Example").Should().Be(2);
                Read("MATCH (emp:Person)-[WORKS_FOR]->(com:Company) WHERE com.name = 'Example' RETURN count(emp)")
                    .Single()[0].As<int>().Should().Be(2);
            }
        }

        public class ServiceUnavailableExample : BaseExample
        {
            private readonly IDriver _baseDriver;

            public ServiceUnavailableExample(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
                : base(output, fixture)
            {
                _baseDriver = Driver;
                Driver = GraphDatabase.Driver("bolt://localhost:8080", AuthTokens.Basic(User, Password),
                    o => o.WithMaxTransactionRetryTime(TimeSpan.FromSeconds(3)));
            }

            protected override void Dispose(bool isDisposing)
            {
                if (!isDisposing)
                    return;

                Driver = _baseDriver;
                base.Dispose(true);
            }

            // tag::service-unavailable[]
            public bool AddItem()
            {
                try
                {
                    using (var session = Driver.Session())
                    {
                        return session.WriteTransaction(
                            tx =>
                            {
                                tx.Run("CREATE (a:Item)");
                                return true;
                            }
                        );
                    }
                }
                catch (ServiceUnavailableException)
                {
                    return false;
                }
            }
            // end::service-unavailable[]

            [RequireServerFact]
            public void TestServiceUnavailableExample()
            {
                AddItem().Should().BeFalse();
            }
        }

        [SuppressMessage("ReSharper", "xUnit1013")]
        public class SessionExample : BaseExample
        {
            public SessionExample(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
                : base(output, fixture)
            {
            }

            // tag::session[]
            public void AddPerson(string name)
            {
                using (var session = Driver.Session())
                {
                    session.Run("CREATE (a:Person {name: $name})", new {name});
                }
            }
            // end::session[]

            [RequireServerFact]
            public void TestSessionExample()
            {
                // Given & When
                AddPerson("Alice");
                // Then
                CountPerson("Alice").Should().Be(1);
            }
        }

        [SuppressMessage("ReSharper", "xUnit1013")]
        public class TransactionFunctionExample : BaseExample
        {
            public TransactionFunctionExample(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
                : base(output, fixture)
            {
            }

            // tag::transaction-function[]
            public void AddPerson(string name)
            {
                using (var session = Driver.Session())
                {
                    session.WriteTransaction(tx => tx.Run("CREATE (a:Person {name: $name})", new {name}));
                }
            }
            // end::transaction-function[]

            [RequireServerFact]
            public void TestTransactionFunctionExample()
            {
                // Given & When
                AddPerson("Alice");
                // Then
                CountPerson("Alice").Should().Be(1);
            }
        }
        
        public class DatabaseSelectionExampleTest : BaseExample
        {
            public DatabaseSelectionExampleTest(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
                : base(output, fixture)
            {
            }

            [RequireEnterpriseEdition("4.0.0", VersionComparison.GreaterThanOrEqualTo)]
            public async void TestUseAnotherDatabaseExample()
            {
                try
                {
                    await DropDatabase(Driver, "examples");
                }
                catch (FatalDiscoveryException ex)
                {
                    // Its a new server instance, the database didn't exist yet
                }

                await CreateDatabase(Driver, "examples");
                
                // Given
                using (var example = new DatabaseSelectionExample(Uri, User, Password))
                {
                    // When
                    example.UseAnotherDatabaseExample();
                    
                    // Then
                    var greetingCount = ReadInt("examples", "MATCH (a:Greeting) RETURN count(a)");
                    greetingCount.Should().Be(1);
                }
            }

            private int ReadInt(string database, string query)
            {
                using (var session = Driver.Session(SessionConfigBuilder.ForDatabase(database)))
                {
                    return session.Run(query).Single()[0].As<int>();
                }
            }

            private class DatabaseSelectionExample : IDisposable
            {
                private readonly IDriver _driver;

                public DatabaseSelectionExample(string uri, string user, string password)
                {
                    _driver = GraphDatabase.Driver(uri, AuthTokens.Basic(user, password));
                }

                public void UseAnotherDatabaseExample()
                {
                    // tag::database-selection[]
                    using (var session = _driver.Session(SessionConfigBuilder.ForDatabase("examples")))
                    {
                        session.Run("CREATE (a:Greeting {message: 'Hello, Example-Database'}) RETURN a").Consume();
                    }

                    void SessionConfig(SessionConfigBuilder configBuilder) =>
                        configBuilder.WithDatabase("examples")
                            .WithDefaultAccessMode(AccessMode.Read)
                            .Build();

                    using (var session = _driver.Session(SessionConfig))
                    {
                        var result = session.Run("MATCH (a:Greeting) RETURN a.message as msg");
                        var msg = result.Single()[0].As<string>();
                        Console.WriteLine(msg);
                    }
                    // end::database-selection[]
                }

                public void Dispose()
                {
                    _driver?.Dispose();
                }
            }
        }

        [SuppressMessage("ReSharper", "xUnit1013")]
        public class PassBookmarksExample : BaseExample
        {
            public PassBookmarksExample(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
                : base(output, fixture)
            {
            }

            // tag::pass-bookmarks[]
            // Create a company node
            private IResult AddCompany(ITransaction tx, string name)
            {
                return tx.Run("CREATE (a:Company {name: $name})", new {name});
            }

            // Create a person node
            private IResult AddPerson(ITransaction tx, string name)
            {
                return tx.Run("CREATE (a:Person {name: $name})", new {name});
            }

            // Create an employment relationship to a pre-existing company node.
            // This relies on the person first having been created.
            private IResult Employ(ITransaction tx, string personName, string companyName)
            {
                return tx.Run(@"MATCH (person:Person {name: $personName}) 
                         MATCH (company:Company {name: $companyName}) 
                         CREATE (person)-[:WORKS_FOR]->(company)", new {personName, companyName});
            }

            // Create a friendship between two people.
            private IResult MakeFriends(ITransaction tx, string name1, string name2)
            {
                return tx.Run(@"MATCH (a:Person {name: $name1}) 
                         MATCH (b:Person {name: $name2})
                         MERGE (a)-[:KNOWS]->(b)", new {name1, name2});
            }

            // Match and display all friendships.
            private int PrintFriendships(ITransaction tx)
            {
                var result = tx.Run("MATCH (a)-[:KNOWS]->(b) RETURN a.name, b.name");

                var count = 0;
                foreach (var record in result)
                {
                    count++;
                    Console.WriteLine($"{record["a.name"]} knows {record["b.name"]}");
                }

                return count;
            }

            public void AddEmployAndMakeFriends()
            {
                // To collect the session bookmarks
                var savedBookmarks = new List<Bookmark>();

                // Create the first person and employment relationship.
                using (var session1 = Driver.Session(o => o.WithDefaultAccessMode(AccessMode.Write)))
                {
                    session1.WriteTransaction(tx => AddCompany(tx, "Wayne Enterprises"));
                    session1.WriteTransaction(tx => AddPerson(tx, "Alice"));
                    session1.WriteTransaction(tx => Employ(tx, "Alice", "Wayne Enterprises"));

                    savedBookmarks.Add(session1.LastBookmark);
                }

                // Create the second person and employment relationship.
                using (var session2 = Driver.Session(o => o.WithDefaultAccessMode(AccessMode.Write)))
                {
                    session2.WriteTransaction(tx => AddCompany(tx, "LexCorp"));
                    session2.WriteTransaction(tx => AddPerson(tx, "Bob"));
                    session2.WriteTransaction(tx => Employ(tx, "Bob", "LexCorp"));

                    savedBookmarks.Add(session2.LastBookmark);
                }

                // Create a friendship between the two people created above.
                using (var session3 = Driver.Session(o =>
                    o.WithDefaultAccessMode(AccessMode.Write).WithBookmarks(savedBookmarks.ToArray())))
                {
                    session3.WriteTransaction(tx => MakeFriends(tx, "Alice", "Bob"));

                    session3.ReadTransaction(PrintFriendships);
                }
            }

            // end::pass-bookmarks[]

            [RequireServerFact]
            public void TestPassBookmarksExample()
            {
                // Given & When
                AddEmployAndMakeFriends();

                // Then
                CountNodes("Person", "name", "Alice").Should().Be(1);
                CountNodes("Person", "name", "Bob").Should().Be(1);
                CountNodes("Company", "name", "Wayne Enterprises").Should().Be(1);
                CountNodes("Company", "name", "LexCorp").Should().Be(1);

                var works1 = Read(
                    "MATCH (a:Person {name: $person})-[:WORKS_FOR]->(b:Company {name: $company}) RETURN count(a)",
                    new {person = "Alice", company = "Wayne Enterprises"});
                works1.Count().Should().Be(1);

                var works2 = Read(
                    "MATCH (a:Person {name: $person})-[:WORKS_FOR]->(b:Company {name: $company}) RETURN count(a)",
                    new {person = "Bob", company = "LexCorp"});
                works2.Count().Should().Be(1);

                var friends = Read(
                    "MATCH (a:Person {name: $person1})-[:KNOWS]->(b:Person {name: $person2}) RETURN count(a)",
                    new {person1 = "Alice", person2 = "Bob"});
                friends.Count().Should().Be(1);
            }
        }
    }

    [Collection(SAIntegrationCollection.CollectionName)]
    public abstract class BaseExample : IDisposable
    {
        protected ITestOutputHelper Output { get; }
        protected IDriver Driver { set; get; }
        protected const string Uri = Neo4jDefaultInstallation.BoltUri;
        protected const string User = Neo4jDefaultInstallation.User;
        protected const string Password = Neo4jDefaultInstallation.Password;

        protected BaseExample(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
        {
            Output = output;
            Driver = fixture.StandAlone.Driver;
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (!isDisposing)
                return;

            using (var session = Driver.Session())
            {
                session.Run("MATCH (n) DETACH DELETE n").Consume();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected int CountNodes(string label, string property, string value)
        {
            using (var session = Driver.Session())
            {
                return session.ReadTransaction(
                    tx => tx.Run($"MATCH (a:{label} {{{property}: $value}}) RETURN count(a)",
                        new {value}).Single()[0].As<int>());
            }
        }

        protected int CountPerson(string name)
        {
            return CountNodes("Person", "name", name);
        }

        protected void Write(string query, object parameters = null)
        {
            using (var session = Driver.Session())
            {
                session.WriteTransaction(tx =>
                    tx.Run(query, parameters));
            }
        }

        protected List<IRecord> Read(string query, object parameters = null)
        {
            using (var session = Driver.Session())
            {
                return session.ReadTransaction(tx =>
                    tx.Run(query, parameters).ToList());
            }
        }
    }
}