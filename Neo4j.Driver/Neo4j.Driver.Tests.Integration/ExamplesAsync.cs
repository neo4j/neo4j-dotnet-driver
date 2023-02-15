// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
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
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.IntegrationTests.Internals;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests;

public class ExamplesAsync
{
    public class AsyncSectionExamples : BaseAsyncExample
    {
        public AsyncSectionExamples(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
            : base(output, fixture)
        {
        }

        // tag::async-autocommit-transaction[]
        public async Task<List<string>> ReadProductTitles()
        {
            var records = new List<string>();
            await using var session = Driver.AsyncSession();

            // Send cypher query to the database.
            // The existing IResult interface implements IEnumerable
            // and does not play well with asynchronous use cases. The replacement
            // IResultCursor interface is returned from the RunAsync
            // family of methods instead and provides async capable methods. 
            var reader = await session.RunAsync(
                "MATCH (p:Product) WHERE p.id = $id RETURN p.title", // Cypher query
                new { id = 0 } // Parameters in the query, if any
            );

            // Loop through the records asynchronously
            while (await reader.FetchAsync())
                // Each current read in buffer can be reached via Current
            {
                records.Add(reader.Current[0].ToString());
            }

            return records;
        }
        // end::async-autocommit-transaction[]

        // tag::async-transaction-function[]
        public async Task<List<string>> PrintAllProducts()
        {
            await using var session = Driver.AsyncSession();
            // Wrap whole operation into an managed transaction and
            // get the results back.
            return await session.ExecuteReadAsync(
                async tx =>
                {
                    var products = new List<string>();

                    // Send cypher query to the database
                    var reader = await tx.RunAsync(
                        "MATCH (p:Product) WHERE p.id = $id RETURN p.title", // Cypher query
                        new { id = 0 } // Parameters in the query, if any
                    );

                    // Loop through the records asynchronously
                    while (await reader.FetchAsync())
                        // Each current read in buffer can be reached via Current
                    {
                        products.Add(reader.Current[0].ToString());
                    }

                    return products;
                });
        }
        // end::async-transaction-function[]

        // tag::async-explicit-transaction[]
        public async Task<string> PrintSingleProduct()
        {
            await using var session = Driver.AsyncSession();

            // Start an explicit transaction
            var tx = await session.BeginTransactionAsync();

            // Send cypher query to the database through the explicit
            // transaction acquired
            var reader = await tx.RunAsync(
                "MATCH (p:Product) WHERE p.id = $id RETURN p.title", // Cypher query
                new { id = 0 } // Parameters in the query, if any
            );

            // Loop through the records asynchronously
            var single = await reader.SingleAsync();
            var product = single[0].ToString();

            // Commit the transaction
            await tx.CommitAsync();

            return product;
        }
        // end::async-explicit-transaction[]

        // tag::async-multiple-tx[]
        public async Task<int?> EmployEveryoneInCompany(string companyName)
        {
            await using var session = Driver.AsyncSession();

            var names = await session.ExecuteReadAsync(
                async tx =>
                {
                    var cursor = await tx.RunAsync("MATCH (a:Person) RETURN a.name AS name");
                    var people = await cursor.ToListAsync();
                    return people.Select(person => person["name"].As<string>());
                });

            return await session.ExecuteWriteAsync(
                async tx =>
                {
                    var relationshipsCreated = new List<int>();
                    foreach (var personName in names)
                    {
                        var cursor = await tx.RunAsync(
                            "MATCH (emp:Person {name: $person_name}) " +
                            "MERGE (com:Company {name: $company_name}) " +
                            "MERGE (emp)-[:WORKS_FOR]->(com)",
                            new
                            {
                                person_name = personName,
                                company_name = companyName
                            });

                        var summary = await cursor.ConsumeAsync();

                        relationshipsCreated.Add(summary.Counters.RelationshipsCreated);
                    }

                    return relationshipsCreated.Sum();
                });
        }
        // end::async-multiple-tx[]

        [RequireServerFact]
        public async void TestAutocommitTransactionExample()
        {
            await WriteAsync(
                "CREATE (p:Product) SET p.id = $id, p.title = $title",
                new { id = 0, title = "Product-0" });

            var results = await ReadProductTitles();

            results.Should().NotBeNull();
            results.Should().HaveCount(1);
            results.Should().Contain("Product-0");
        }

        [RequireServerFact]
        public async void TestTransactionFunctionExample()
        {
            await WriteAsync(
                "CREATE (p:Product) SET p.id = $id, p.title = $title",
                new { id = 0, title = "Product-0" });

            var results = await PrintAllProducts();

            results.Should().NotBeNull();
            results.Should().HaveCount(1);
            results.Should().Contain("Product-0");
        }

        [RequireServerFact]
        public async void TestExplicitTransactionExample()
        {
            await WriteAsync(
                "CREATE (p:Product) SET p.id = $id, p.title = $title",
                new { id = 0, title = "Product-0" });

            var result = await PrintSingleProduct();

            result.Should().NotBeNull();
            result.Should().Contain("Product-0");
        }

        [RequireServerFact]
        public async void TestAsyncMultipleTxExample()
        {
            await WriteAsync(
                "CREATE (a:Person {name: $nameA}), (b:Person {name: $nameB})",
                new { nameA = "Alice", nameB = "Bob" });

            var result = await EmployEveryoneInCompany("Acme");

            result.Should().NotBeNull();
            result.Should().Be(2);
        }
    }

    [SuppressMessage("ReSharper", "xUnit1013")]
    public class AutocommitTransactionExample : BaseAsyncExample
    {
        public AutocommitTransactionExample(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
            : base(output, fixture)
        {
        }

        public async Task AddPersonAsync(string name)
        {
            await using var session = Driver.AsyncSession();
            await session.RunAsync("CREATE (a:Person {name: $name})", new { name });
        }

        [RequireServerFact]
        public async Task TestAutocommitTransactionExample()
        {
            // Given & When
            await AddPersonAsync("Alice");
            // Then
            var count = await CountPersonAsync("Alice");

            count.Should().Be(1);
        }
    }

    public class BasicAuthExample : BaseAsyncExample
    {
        public BasicAuthExample(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
            : base(output, fixture)
        {
        }

        public IDriver CreateDriverWithBasicAuth(string uri, string user, string password)
        {
            return GraphDatabase.Driver(uri, AuthTokens.Basic(user, password));
        }

        [RequireServerFact]
        public async Task TestBasicAuthExample()
        {
            // Given
            var driver = CreateDriverWithBasicAuth(Uri, User, Password);
            await using var session = driver.AsyncSession();
            // When & Then
            var result = await session.RunAsync("RETURN 1");

            var read = await result.FetchAsync();
            read.Should().BeTrue();

            result.Current[0].As<int>().Should().Be(1);
        }
    }

    public class ConfigConnectionTimeoutExample : BaseAsyncExample
    {
        public ConfigConnectionTimeoutExample(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
            : base(output, fixture)
        {
        }

        public IDriver CreateDriverWithCustomizedConnectionTimeout(string uri, string user, string password)
        {
            return GraphDatabase.Driver(
                uri,
                AuthTokens.Basic(user, password),
                o => o.WithConnectionTimeout(TimeSpan.FromSeconds(15)));
        }

        [RequireServerFact]
        public async Task TestConfigConnectionTimeoutExample()
        {
            // Given
            var driver = CreateDriverWithCustomizedConnectionTimeout(Uri, User, Password);
            await using var session = driver.AsyncSession();
            // When & Then
            var result = await session.RunAsync("RETURN 1");

            var read = await result.FetchAsync();
            read.Should().BeTrue();

            result.Current[0].As<int>().Should().Be(1);
        }
    }

    public class ConfigMaxRetryTimeExample : BaseAsyncExample
    {
        public ConfigMaxRetryTimeExample(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
            : base(output, fixture)
        {
        }

        public IDriver CreateDriverWithCustomizedMaxRetryTime(string uri, string user, string password)
        {
            return GraphDatabase.Driver(
                uri,
                AuthTokens.Basic(user, password),
                o => o.WithMaxTransactionRetryTime(TimeSpan.FromSeconds(15)));
        }

        [RequireServerFact]
        public async Task TestConfigMaxRetryTimeExample()
        {
            // Given
            var driver = CreateDriverWithCustomizedMaxRetryTime(Uri, User, Password);
            await using var session = driver.AsyncSession();
            // When & Then
            var result = await session.RunAsync("RETURN 1");

            var read = await result.FetchAsync();
            read.Should().BeTrue();

            result.Current[0].As<int>().Should().Be(1);
        }
    }

    public class ConfigTrustExample : BaseAsyncExample
    {
        public ConfigTrustExample(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
            : base(output, fixture)
        {
        }

        public IDriver CreateDriverWithCustomizedTrustStrategy(string uri, string user, string password)
        {
            return GraphDatabase.Driver(
                uri,
                AuthTokens.Basic(user, password),
                o => o.WithTrustManager(TrustManager.CreateInsecure()));
        }

        [RequireServerFact]
        public async Task TestConfigTrustExample()
        {
            // Given
            var driver = CreateDriverWithCustomizedTrustStrategy(Uri, User, Password);
            await using var session = driver.AsyncSession();
            // When & Then
            var result = await session.RunAsync("RETURN 1");

            var read = await result.FetchAsync();
            read.Should().BeTrue();

            result.Current[0].As<int>().Should().Be(1);
        }
    }

    public class ConfigUnencryptedExample : BaseAsyncExample
    {
        public ConfigUnencryptedExample(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
            : base(output, fixture)
        {
        }

        public IDriver CreateDriverWithCustomizedSecurityStrategy(string uri, string user, string password)
        {
            return GraphDatabase.Driver(
                uri,
                AuthTokens.Basic(user, password),
                o => o.WithEncryptionLevel(EncryptionLevel.None));
        }

        [RequireServerFact]
        public async Task TestConfigUnencryptedExample()
        {
            // Given
            var driver = CreateDriverWithCustomizedSecurityStrategy(Uri, User, Password);
            await using var session = driver.AsyncSession();
            // When & Then
            var result = await session.RunAsync("RETURN 1");

            var read = await result.FetchAsync();
            read.Should().BeTrue();

            result.Current[0].As<int>().Should().Be(1);
        }
    }

    public class CustomAuthExample : BaseAsyncExample
    {
        public CustomAuthExample(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
            : base(output, fixture)
        {
        }

        public IDriver CreateDriverWithCustomizedAuth(
            string uri,
            string principal,
            string credentials,
            string realm,
            string scheme,
            Dictionary<string, object> parameters)
        {
            return GraphDatabase.Driver(
                uri,
                AuthTokens.Custom(principal, credentials, realm, scheme, parameters),
                o => o.WithEncryptionLevel(EncryptionLevel.None));
        }

        [RequireServerFact]
        public async Task TestCustomAuthExample()
        {
            // Given
            var driver = CreateDriverWithCustomizedAuth(Uri, User, Password, "native", "basic", null);
            await using var session = driver.AsyncSession();
            // When & Then
            var result = await session.RunAsync("RETURN 1");

            var read = await result.FetchAsync();
            read.Should().BeTrue();

            result.Current[0].As<int>().Should().Be(1);
        }
    }

    public class BearerAuthExample : BaseAsyncExample
    {
        public BearerAuthExample(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
            : base(output, fixture)
        {
        }

        public IDriver CreateDriverWithBearerAuth(string uri, string bearerToken)
        {
            return GraphDatabase.Driver(
                uri,
                AuthTokens.Bearer(bearerToken),
                o => o.WithEncryptionLevel(EncryptionLevel.None));
        }
    }

    public class CypherErrorExample : BaseAsyncExample
    {
        public CypherErrorExample(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
            : base(output, fixture)
        {
        }

        public async Task<int> GetEmployeeNumberAsync(string name)
        {
            await using var session = Driver.AsyncSession();
            return await session.ExecuteReadAsync(async tx => await SelectEmployee(tx, name));
        }

        private async Task<int> SelectEmployee(IAsyncQueryRunner tx, string name)
        {
            try
            {
                var result = await tx.RunAsync("SELECT * FROM Employees WHERE name = $name", new { name });
                return (await result.SingleAsync())["employee_number"].As<int>();
            }
            catch (ClientException ex)
            {
                Output.WriteLine(ex.Message);
                return -1;
            }
        }

        [RequireServerFact]
        public async Task TestCypherErrorExample()
        {
            // When & Then
            var result = await GetEmployeeNumberAsync("Alice");

            result.Should().Be(-1);
        }
    }

    public class DriverLifecycleExampleTest : BaseAsyncExample
    {
        public DriverLifecycleExampleTest(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
            : base(output, fixture)
        {
        }

        [RequireServerFact]
        public async Task TestDriverLifecycleExample()
        {
            // Given
            var driver = new DriverLifecycleExample(Uri, User, Password).Driver;
            await using var session = driver.AsyncSession();
            // When & Then
            var result = await session.RunAsync("RETURN 1");

            var read = await result.FetchAsync();
            read.Should().BeTrue();

            result.Current[0].As<int>().Should().Be(1);
        }

        public class DriverLifecycleExample : IDisposable
        {
            private bool _disposed;

            public DriverLifecycleExample(string uri, string user, string password)
            {
                Driver = GraphDatabase.Driver(uri, AuthTokens.Basic(user, password));
            }

            public IDriver Driver { get; }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            ~DriverLifecycleExample()
            {
                Dispose(false);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (_disposed)
                {
                    return;
                }

                if (disposing)
                {
                    Driver?.Dispose();
                }

                _disposed = true;
            }
        }
    }
    public class HelloWorldExampleTest : BaseAsyncExample
    {
        public HelloWorldExampleTest(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
            : base(output, fixture)
        {
        }

        [RequireServerFact]
        public async Task TestHelloWorldExample()
        {
            // Given
            using var example = new HelloWorldExample(Uri, User, Password);
            // When & Then
            await example.PrintGreetingAsync("Hello, world");
        }
        
        // tag::async-hello-world[]
        public class HelloWorldExample : IDisposable
        {
            private readonly IDriver _driver;

            public HelloWorldExample(string uri, string user, string password)
            {
                _driver = GraphDatabase.Driver(uri, AuthTokens.Basic(user, password));
            }

            public async Task PrintGreetingAsync(string message)
            {
                await using var session = _driver.AsyncSession();
                var greeting = await session.ExecuteWriteAsync(
                    async tx =>
                    {
                        var result = await tx.RunAsync(
                            "CREATE (a:Greeting) " +
                            "SET a.message = $message " +
                            "RETURN a.message + ', from node ' + id(a)",
                            new { message });

                        var record = await result.SingleAsync();
                        return record[0].As<string>();
                    });

                Console.WriteLine(greeting);
            }

            public void Dispose()
            {
                _driver?.Dispose();
            }
        }
        // end::async-hello-world[]
    }

    public class DriverIntroductionExampleTest : BaseAsyncExample
    {
        public DriverIntroductionExampleTest(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
            : base(output, fixture)
        {
        }

        [RequireServerFact]
        public async Task TestDriverIntroductionExample()
        {
            // Given
            using var example = new DriverIntroductionExample(Uri, User, Password);
            // When & Then
            await example.CreateFriendship("Alice", "David", "School");
            await example.FindPerson("Alice");
        }

        // tag::driver-introduction-example[]
        public class DriverIntroductionExample : IDisposable
        {
            private readonly IDriver _driver;
            private bool _disposed;

            public DriverIntroductionExample(string uri, string user, string password)
            {
                _driver = GraphDatabase.Driver(
                    uri,
                    AuthTokens.Basic(user, password),
                    configBuilder => configBuilder.WithLogger(new SimpleLogger()));
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            ~DriverIntroductionExample()
            {
                Dispose(false);
            }

            public async Task CreateFriendship(string person1Name, string person2Name, string knowsFrom)
            {
                // To learn more about the Cypher syntax, see https://neo4j.com/docs/cypher-manual/current/
                // The Reference Card is also a good resource for keywords https://neo4j.com/docs/cypher-refcard/current/
                var query = @"
                    MERGE (p1:Person { name: $person1Name })
                    MERGE (p2:Person { name: $person2Name })
                    MERGE (p1)-[k:KNOWS { from: $knowsFrom }]->(p2)
                    RETURN p1, p2, k";

                await using var session = _driver.AsyncSession();
                try
                {
                    // Write transactions allow the driver to handle retries and transient error
                    var writeResults = await session.ExecuteWriteAsync(
                        async tx =>
                        {
                            var result = await tx.RunAsync(query, new { person1Name, person2Name, knowsFrom });
                            return await result.ToListAsync();
                        });

                    foreach (var result in writeResults)
                    {
                        var person1 = result["p1"].As<INode>().Properties["name"];
                        var person2 = result["p2"].As<INode>().Properties["name"];
                        var from = result["k"].As<IRelationship>().Properties["from"];
                        Console.WriteLine($"Created friendship between: {person1}, {person2} from {from}");
                    }
                }
                // Capture any errors along with the query and data for traceability
                catch (Neo4jException ex)
                {
                    Console.WriteLine($"{query} - {ex}");
                    throw;
                }
            }

            public async Task FindPerson(string personName)
            {
                var query = @"
                    MATCH (p:Person)
                    WHERE p.name = $name
                    RETURN p.name";

                await using var session = _driver.AsyncSession();
                try
                {
                    var readResults = await session.ExecuteReadAsync(
                        async tx =>
                        {
                            var result = await tx.RunAsync(query, new { name = personName });
                            return await result.ToListAsync();
                        });

                    foreach (var result in readResults)
                    {
                        Console.WriteLine($"Found person: {result["p.name"].As<string>()}");
                    }
                }
                // Capture any errors along with the query and data for traceability
                catch (Neo4jException ex)
                {
                    Console.WriteLine($"{query} - {ex}");
                    throw;
                }
            }

            protected virtual void Dispose(bool disposing)
            {
                if (_disposed)
                {
                    return;
                }

                if (disposing)
                {
                    _driver?.Dispose();
                }

                _disposed = true;
            }

#pragma warning disable CS8892
            public static async Task Main(string[] args)
#pragma warning restore CS8892
            {
                // Aura queries use an encrypted connection using the "neo4j+s" protocol
                var boltUrl = "%%BOLT_URL_PLACEHOLDER%%";

                var user = "<Username for Neo4j Aura database>";
                var password = "<Password for Neo4j Aura database>";

                using var example = new DriverIntroductionExample(boltUrl, user, password);
                await example.CreateFriendship("Alice", "David", "School");
                await example.FindPerson("Alice");
            }
        }

        internal class SimpleLogger : ILogger
        {
            public void Debug(string message, params object[] args)
            {
                Console.WriteLine("[DRIVER-DEBUG]" + message, args);
            }

            public void Error(Exception error, string message, params object[] args)
            {
                Console.WriteLine("[DRIVER-ERROR]" + message, args);
            }

            public void Info(string message, params object[] args)
            {
                Console.WriteLine("[DRIVER-INFO]" + message, args);
            }

            public bool IsDebugEnabled()
            {
                return false;
            }

            public bool IsTraceEnabled()
            {
                return false;
            }

            public void Trace(string message, params object[] args)
            {
                Console.WriteLine("[DRIVER-TRACE]" + message, args);
            }

            public void Warn(Exception error, string message, params object[] args)
            {
                Console.WriteLine("[DRIVER-WARN]" + message, args);
            }
        }
        // end::driver-introduction-example[]
    }

    public class ReadWriteTransactionExample : BaseAsyncExample
    {
        public ReadWriteTransactionExample(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
            : base(output, fixture)
        {
        }

        [RequireServerFact]
        public async Task TestReadWriteTransactionExample()
        {
            // When & Then
            var id = await AddPersonAsync("Alice");

            id.Should().BeGreaterOrEqualTo(0L);
        }

        public async Task<long> AddPersonAsync(string name)
        {
            await using var session = Driver.AsyncSession();
            await session.ExecuteWriteAsync(tx => CreatePersonNodeAsync(tx, name));
            return await session.ExecuteReadAsync(tx => MatchPersonNodeAsync(tx, name));
        }

        private static Task CreatePersonNodeAsync(IAsyncQueryRunner tx, string name)
        {
            return tx.RunAsync("CREATE (a:Person {name: $name})", new { name });
        }

        private static async Task<long> MatchPersonNodeAsync(IAsyncQueryRunner tx, string name)
        {
            var result = await tx.RunAsync("MATCH (a:Person {name: $name}) RETURN id(a)", new { name });
            return (await result.SingleAsync())[0].As<long>();
        }
    }

    public class ResultConsumeExample : BaseAsyncExample
    {
        public ResultConsumeExample(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
            : base(output, fixture)
        {
        }

        // tag::async-result-consume[]
        public async Task<List<string>> GetPeopleAsync()
        {
            await using var session = Driver.AsyncSession();
            return await session.ExecuteReadAsync(
                async tx =>
                {
                    var result = await tx.RunAsync("MATCH (a:Person) RETURN a.name ORDER BY a.name");
                    return await result.ToListAsync(r => r[0].As<string>());
                });
        }
        // end::async-result-consume[]

        [RequireServerFact]
        public async Task TestResultConsumeExample()
        {
            // Given
            await WriteAsync("CREATE (a:Person {name: 'Alice'})");
            await WriteAsync("CREATE (a:Person {name: 'Bob'})");
            // When & Then
            var people = await GetPeopleAsync();

            people.Should().Contain(new[] { "Alice", "Bob" });
        }
    }

    public class ResultRetainExample : BaseAsyncExample
    {
        public ResultRetainExample(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
            : base(output, fixture)
        {
        }

        public async Task<int> AddEmployeesAsync(string companyName)
        {
            await using var session = Driver.AsyncSession();
            var persons = await session.ExecuteReadAsync(
                async tx =>
                {
                    var cursor = await tx.RunAsync("MATCH (a:Person) RETURN a.name AS name");
                    return await cursor.ToListAsync();
                });

            var personSums = new List<int>();
            foreach (var person in persons)
            {
                var result = await session.ExecuteWriteAsync(
                    async tx =>
                    {
                        var cursor = await tx.RunAsync(
                            "MATCH (emp:Person {name: $person_name}) " +
                            "MERGE (com:Company {name: $company_name}) " +
                            "MERGE (emp)-[:WORKS_FOR]->(com)",
                            new
                            {
                                person_name = person["name"].As<string>(),
                                company_name = companyName
                            });

                        await cursor.ConsumeAsync();

                        return 1;
                    });

                personSums.Add(result);
            }

            return personSums.Sum();
        }

        [RequireServerFact]
        public async Task TestResultConsumeExample()
        {
            // Given
            await WriteAsync("CREATE (a:Person {name: 'Alice'})");
            await WriteAsync("CREATE (a:Person {name: 'Bob'})");
            // When & Then
            var count = await AddEmployeesAsync("Example");
            count.Should().Be(2);

            var records = await ReadAsync(
                "MATCH (emp:Person)-[WORKS_FOR]->(com:Company) WHERE com.name = 'Example' RETURN count(emp)");

            var record = records.Single();
            record[0].As<int>().Should().Be(2);
        }
    }

    public class ServiceUnavailableExample : BaseAsyncExample
    {
        private readonly IDriver _baseDriver;

        public ServiceUnavailableExample(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
            : base(output, fixture)
        {
            _baseDriver = Driver;
            Driver = GraphDatabase.Driver(
                "bolt://localhost:8080",
                AuthTokens.Basic(User, Password),
                o => o.WithMaxTransactionRetryTime(TimeSpan.FromSeconds(3)));
        }

        protected override void Dispose(bool isDisposing)
        {
            if (!isDisposing)
            {
                return;
            }

            Driver = _baseDriver;
            base.Dispose(true);
        }

        public async Task<bool> AddItemAsync()
        {
            await using var session = Driver.AsyncSession();
            try
            {
                return await session.ExecuteWriteAsync(
                    async tx =>
                    {
                        await tx.RunAsync("CREATE (a:Item)");
                        return true;
                    });
            }
            catch (ServiceUnavailableException)
            {
                return false;
            }
        }

        [RequireServerFact]
        public async Task TestServiceUnavailableExample()
        {
            var result = await AddItemAsync();

            result.Should().BeFalse();
        }
    }

    [SuppressMessage("ReSharper", "xUnit1013")]
    public class SessionExample : BaseAsyncExample
    {
        public SessionExample(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
            : base(output, fixture)
        {
        }

        public async Task AddPersonAsync(string name)
        {
            await using var session = Driver.AsyncSession();
            await session.RunAsync("CREATE (a:Person {name: $name})", new { name });
        }

        [RequireServerFact]
        public async Task TestSessionExample()
        {
            // Given & When
            await AddPersonAsync("Alice");
            // Then
            var count = await CountPersonAsync("Alice");
            count.Should().Be(1);
        }
    }

    [SuppressMessage("ReSharper", "xUnit1013")]
    public class TransactionFunctionExample : BaseAsyncExample
    {
        public TransactionFunctionExample(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
            : base(output, fixture)
        {
        }

        public async Task AddPersonAsync(string name)
        {
            await using var session = Driver.AsyncSession();
            await session.ExecuteWriteAsync(tx => tx.RunAsync("CREATE (a:Person {name: $name})", new { name }));
        }

        [RequireServerFact]
        public async void TestTransactionFunctionExample()
        {
            // Given & When
            await AddPersonAsync("Alice");
            // Then
            var count = await CountPersonAsync("Alice");

            count.Should().Be(1);
        }
    }
}

[Collection(SaIntegrationCollection.CollectionName)]
public abstract class BaseAsyncExample : IDisposable
{
    private bool _disposed;
    protected string Password = DefaultInstallation.Password;
    protected string Uri = DefaultInstallation.BoltUri;
    protected string User = DefaultInstallation.User;

    protected BaseAsyncExample(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
    {
        Output = output;
        Driver = fixture.StandAloneSharedInstance.Driver;
    }

    protected ITestOutputHelper Output { get; }
    protected IDriver Driver { set; get; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~BaseAsyncExample()
    {
        Dispose(false);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            using var session = Driver.Session();
            session.Run("MATCH (n) DETACH DELETE n").Consume();
        }

        _disposed = true;
    }

    protected async Task<int> CountPersonAsync(string name)
    {
        var session = Driver.AsyncSession();
        try
        {
            return await session.ExecuteReadAsync(
                async tx =>
                {
                    var result = await tx.RunAsync("MATCH (a:Person {name: $name}) RETURN count(a)", new { name });

                    return (await result.SingleAsync())[0].As<int>();
                });
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    protected async Task WriteAsync(string query, object parameters)
    {
        await using var session = Driver.AsyncSession();
        await session.ExecuteWriteAsync(tx => tx.RunAsync(query, parameters));
    }

    protected async Task WriteAsync(string query, IDictionary<string, object> parameters = null)
    {
        await using var session = Driver.AsyncSession();
        await session.ExecuteWriteAsync(async tx => await tx.RunAsync(query, parameters));
    }

    protected async Task<List<IRecord>> ReadAsync(
        string query,
        IDictionary<string, object> parameters = null)
    {
        await using var session = Driver.AsyncSession();
        return await session.ExecuteReadAsync(
            async tx =>
            {
                var cursor = await tx.RunAsync(query, parameters);
                return await cursor.ToListAsync();
            });
    }
}
