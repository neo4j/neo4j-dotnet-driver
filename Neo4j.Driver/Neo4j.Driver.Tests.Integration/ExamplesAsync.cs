// Copyright (c) 2002-2019 "Neo4j,"
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

using FluentAssertions;
using Neo4j.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neo4j.Driver.IntegrationTests;
using Neo4j.Driver.IntegrationTests.Internals;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.ExamplesAsync
{
    public class ExamplesAsync
    {
        public class AsyncSectionExamples : BaseAsyncExample
        {
            public AsyncSectionExamples(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
                : base(output, fixture)
            {
            }

            public async Task<List<string>> AutocommitTransactionExample()
            {
                // tag::async-autocommit-transaction[]
                var records = new List<string>();
                var session = Driver.AsyncSession();

                try
                {
                    // Send cypher statement to the database.
                    // The existing IStatementResult interface implements IEnumerable
                    // and does not play well with asynchronous use cases. The replacement
                    // IStatementResultCursor interface is returned from the RunAsync
                    // family of methods instead and provides async capable methods. 
                    var reader = await session.RunAsync(
                        "MATCH (p:Product) WHERE p.id = $id RETURN p.title", // Cypher statement
                        new {id = 0} // Parameters in the statement, if any
                    );

                    // Loop through the records asynchronously
                    while (await reader.FetchAsync())
                    {
                        // Each current read in buffer can be reached via Current
                        records.Add(reader.Current[0].ToString());
                    }
                }
                finally
                {
                    // asynchronously close session
                    await session.CloseAsync();
                }

                // end::async-autocommit-transaction[]
                return records;
            }

            public async Task<List<string>> TransactionFunctionExample()
            {
                List<string> result = null;
                // tag::async-transaction-function[]
                var session = Driver.AsyncSession();

                try
                {
                    // Wrap whole operation into an implicit transaction and
                    // get the results back.
                    result = await session.ReadTransactionAsync(async tx =>
                    {
                        var records = new List<string>();

                        // Send cypher statement to the database
                        var reader = await tx.RunAsync(
                            "MATCH (p:Product) WHERE p.id = $id RETURN p.title", // Cypher statement
                            new {id = 0} // Parameters in the statement, if any
                        );

                        // Loop through the records asynchronously
                        while (await reader.FetchAsync())
                        {
                            // Each current read in buffer can be reached via Current
                            records.Add(reader.Current[0].ToString());
                        }

                        return records;
                    });
                }
                finally
                {
                    // asynchronously close session
                    await session.CloseAsync();
                }

                // end::async-transaction-function[]
                return result;
            }

            public async Task<List<string>> ExplicitTransactionExample()
            {
                // tag::async-explicit-transaction[]
                var records = new List<string>();
                var session = Driver.AsyncSession();

                try
                {
                    // Start an explicit transaction
                    var tx = await session.BeginTransactionAsync();

                    // Send cypher statement to the database through the explicit
                    // transaction acquired
                    var reader = await tx.RunAsync(
                        "MATCH (p:Product) WHERE p.id = $id RETURN p.title", // Cypher statement
                        new {id = 0} // Parameters in the statement, if any
                    );

                    // Loop through the records asynchronously
                    while (await reader.FetchAsync())
                    {
                        // Each current read in buffer can be reached via Current
                        records.Add(reader.Current[0].ToString());
                    }

                    // Commit the transaction
                    await tx.CommitAsync();
                }
                finally
                {
                    // asynchronously close session
                    await session.CloseAsync();
                }

                // end::async-explicit-transaction[]
                return records;
            }

            [RequireServerFact]
            public async void TestAutocommitTransactionExample()
            {
                await WriteAsync("CREATE (p:Product) SET p.id = $id, p.title = $title",
                    new {id = 0, title = "Product-0"});

                var results = await AutocommitTransactionExample();

                results.Should().NotBeNull();
                results.Should().HaveCount(1);
                results.Should().Contain("Product-0");
            }

            [RequireServerFact]
            public async void TestTransactionFunctionExample()
            {
                await WriteAsync("CREATE (p:Product) SET p.id = $id, p.title = $title",
                    new {id = 0, title = "Product-0"});

                var results = await TransactionFunctionExample();

                results.Should().NotBeNull();
                results.Should().HaveCount(1);
                results.Should().Contain("Product-0");
            }

            [RequireServerFact]
            public async void TestExplicitTransactionExample()
            {
                await WriteAsync("CREATE (p:Product) SET p.id = $id, p.title = $title",
                    new {id = 0, title = "Product-0"});

                var results = await ExplicitTransactionExample();

                results.Should().NotBeNull();
                results.Should().HaveCount(1);
                results.Should().Contain("Product-0");
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
                var session = Driver.AsyncSession();
                try
                {
                    await session.RunAsync("CREATE (a:Person {name: $name})", new {name});
                }
                finally
                {
                    await session.CloseAsync();
                }
            }

            [RequireServerFact]
            public async Task TestAutocommitTransactionExample()
            {
                // Given & When
                await AddPersonAsync("Alice");
                // Then
                int count = await CountPersonAsync("Alice");

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
                var session = driver.AsyncSession();
                try
                {
                    // When & Then
                    IStatementResultCursor result = await session.RunAsync("RETURN 1");

                    bool read = await result.FetchAsync();
                    read.Should().BeTrue();

                    result.Current[0].As<int>().Should().Be(1);
                }
                finally
                {
                    await session.CloseAsync();
                }
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
                return GraphDatabase.Driver(uri, AuthTokens.Basic(user, password),
                    new Config {ConnectionTimeout = TimeSpan.FromSeconds(15)});
            }

            [RequireServerFact]
            public async Task TestConfigConnectionTimeoutExample()
            {
                // Given
                var driver = CreateDriverWithCustomizedConnectionTimeout(Uri, User, Password);
                var session = driver.AsyncSession();
                try
                {
                    // When & Then
                    IStatementResultCursor result = await session.RunAsync("RETURN 1");

                    bool read = await result.FetchAsync();
                    read.Should().BeTrue();

                    result.Current[0].As<int>().Should().Be(1);
                }
                finally
                {
                    await session.CloseAsync();
                }
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
                return GraphDatabase.Driver(uri, AuthTokens.Basic(user, password),
                    new Config {MaxTransactionRetryTime = TimeSpan.FromSeconds(15)});
            }

            [RequireServerFact]
            public async Task TestConfigMaxRetryTimeExample()
            {
                // Given
                var driver = CreateDriverWithCustomizedMaxRetryTime(Uri, User, Password);
                var session = driver.AsyncSession();
                try
                {
                    // When & Then
                    IStatementResultCursor result = await session.RunAsync("RETURN 1");

                    bool read = await result.FetchAsync();
                    read.Should().BeTrue();

                    result.Current[0].As<int>().Should().Be(1);
                }
                finally
                {
                    await session.CloseAsync();
                }
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
                return GraphDatabase.Driver(uri, AuthTokens.Basic(user, password),
                    new Config {TrustManager = TrustManager.CreateInsecure()});
            }

            [RequireServerFact]
            public async Task TestConfigTrustExample()
            {
                // Given
                var driver = CreateDriverWithCustomizedTrustStrategy(Uri, User, Password);
                var session = driver.AsyncSession();
                try
                {
                    // When & Then
                    IStatementResultCursor result = await session.RunAsync("RETURN 1");

                    bool read = await result.FetchAsync();
                    read.Should().BeTrue();

                    result.Current[0].As<int>().Should().Be(1);
                }
                finally
                {
                    await session.CloseAsync();
                }
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
                return GraphDatabase.Driver(uri, AuthTokens.Basic(user, password),
                    new Config {EncryptionLevel = EncryptionLevel.None});
            }

            [RequireServerFact]
            public async Task TestConfigUnencryptedExample()
            {
                // Given
                var driver = CreateDriverWithCustomizedSecurityStrategy(Uri, User, Password);
                var session = driver.AsyncSession();
                try
                {
                    // When & Then
                    IStatementResultCursor result = await session.RunAsync("RETURN 1");

                    bool read = await result.FetchAsync();
                    read.Should().BeTrue();

                    result.Current[0].As<int>().Should().Be(1);
                }
                finally
                {
                    await session.CloseAsync();
                }
            }
        }

        public class CustomAuthExample : BaseAsyncExample
        {
            public CustomAuthExample(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
                : base(output, fixture)
            {
            }

            public IDriver CreateDriverWithCustomizedAuth(string uri,
                string principal, string credentials, string realm, string scheme,
                Dictionary<string, object> parameters)
            {
                return GraphDatabase.Driver(uri, AuthTokens.Custom(principal, credentials, realm, scheme, parameters),
                    new Config {EncryptionLevel = EncryptionLevel.None});
            }

            [RequireServerFact]
            public async Task TestCustomAuthExample()
            {
                // Given
                var driver = CreateDriverWithCustomizedAuth(Uri, User, Password, "native", "basic", null);
                var session = driver.AsyncSession();
                try
                {
                    // When & Then
                    IStatementResultCursor result = await session.RunAsync("RETURN 1");

                    bool read = await result.FetchAsync();
                    read.Should().BeTrue();

                    result.Current[0].As<int>().Should().Be(1);
                }
                finally
                {
                    await session.CloseAsync();
                }
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
                var session = Driver.AsyncSession();
                try
                {
                    return await session.ReadTransactionAsync(async tx => await SelectEmployee(tx, name));
                }
                finally
                {
                    await session.CloseAsync();
                }
            }

            private async Task<int> SelectEmployee(IAsyncTransaction tx, string name)
            {
                try
                {
                    var result = await tx.RunAsync("SELECT * FROM Employees WHERE name = $name", new {name});

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

            [RequireServerFact]
            public async Task TestDriverLifecycleExample()
            {
                // Given
                var driver = new DriverLifecycleExample(Uri, User, Password).Driver;
                var session = driver.AsyncSession();
                try
                {
                    // When & Then
                    IStatementResultCursor result = await session.RunAsync("RETURN 1");

                    bool read = await result.FetchAsync();
                    read.Should().BeTrue();

                    result.Current[0].As<int>().Should().Be(1);
                }
                finally
                {
                    await session.CloseAsync();
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
                using (var example = new HelloWorldExample(Uri, User, Password))
                {
                    // When & Then
                    await example.PrintGreetingAsync("Hello, world");
                }
            }

            public class HelloWorldExample : IDisposable
            {
                private readonly IDriver _driver;

                public HelloWorldExample(string uri, string user, string password)
                {
                    _driver = GraphDatabase.Driver(uri, AuthTokens.Basic(user, password));
                }

                public async Task PrintGreetingAsync(string message)
                {
                    var session = _driver.AsyncSession();
                    try
                    {
                        var greeting = await session.WriteTransactionAsync(async tx =>
                        {
                            var result = await tx.RunAsync("CREATE (a:Greeting) " +
                                                           "SET a.message = $message " +
                                                           "RETURN a.message + ', from node ' + id(a)",
                                new {message});

                            return (await result.SingleAsync())[0].As<string>();
                        });

                        Console.WriteLine(greeting);
                    }
                    finally
                    {
                        await session.CloseAsync();
                    }
                }

                public void Dispose()
                {
                    _driver?.Dispose();
                }
            }
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
                long id = await AddPersonAsync("Alice");

                id.Should().BeGreaterOrEqualTo(0L);
            }

            public async Task<long> AddPersonAsync(string name)
            {
                var session = Driver.AsyncSession();
                try
                {
                    await session.WriteTransactionAsync(async tx => await CreatePersonNodeAsync(tx, name));

                    return await session.ReadTransactionAsync(tx => MatchPersonNodeAsync(tx, name));
                }
                finally
                {
                    await session.CloseAsync();
                }
            }

            private static Task CreatePersonNodeAsync(IAsyncTransaction tx, string name)
            {
                return tx.RunAsync("CREATE (a:Person {name: $name})", new {name});
            }

            private static async Task<long> MatchPersonNodeAsync(IAsyncTransaction tx, string name)
            {
                var result = await tx.RunAsync("MATCH (a:Person {name: $name}) RETURN id(a)", new {name});


                return (await result.SingleAsync())[0].As<long>();
            }
        }

        public class ResultConsumeExample : BaseAsyncExample
        {
            public ResultConsumeExample(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
                : base(output, fixture)
            {
            }

            public async Task<List<string>> GetPeopleAsync()
            {
                var session = Driver.AsyncSession();
                try
                {
                    return await session.ReadTransactionAsync(async tx =>
                    {
                        var result = await tx.RunAsync("MATCH (a:Person) RETURN a.name ORDER BY a.name");

                        return await result.ToListAsync(r => r[0].As<string>());
                    });
                }
                finally
                {
                    await session.CloseAsync();
                }
            }

            [RequireServerFact]
            public async Task TestResultConsumeExample()
            {
                // Given
                await WriteAsync("CREATE (a:Person {name: 'Alice'})");
                await WriteAsync("CREATE (a:Person {name: 'Bob'})");
                // When & Then
                List<string> people = await GetPeopleAsync();

                people.Should().Contain(new[] {"Alice", "Bob"});
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
                var session = Driver.AsyncSession();
                try
                {
                    var persons = await session.ReadTransactionAsync(async tx =>
                    {
                        var cursor = await tx.RunAsync("MATCH (a:Person) RETURN a.name AS name");
                        return await cursor.ToListAsync();
                    });

                    return persons.Sum(person => session.WriteTransactionAsync(async tx =>
                    {
                        var cursor = await tx.RunAsync("MATCH (emp:Person {name: $person_name}) " +
                                                       "MERGE (com:Company {name: $company_name}) " +
                                                       "MERGE (emp)-[:WORKS_FOR]->(com)",
                            new
                            {
                                person_name = Neo4j.Driver.ValueExtensions.As<string>(person["name"]),
                                company_name = companyName
                            });
                        await cursor.SummaryAsync();

                        return 1;
                    }).Result);
                }
                finally
                {
                    await session.CloseAsync();
                }
            }

            [RequireServerFact]
            public async Task TestResultConsumeExample()
            {
                // Given
                await WriteAsync("CREATE (a:Person {name: 'Alice'})");
                await WriteAsync("CREATE (a:Person {name: 'Bob'})");
                // When & Then
                int count = await AddEmployeesAsync("Acme");
                count.Should().Be(2);

                var records =
                    await ReadAsync(
                        "MATCH (emp:Person)-[WORKS_FOR]->(com:Company) WHERE com.name = 'Acme' RETURN count(emp)");

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
                Driver = GraphDatabase.Driver("bolt://localhost:8080", AuthTokens.Basic(User, Password),
                    new Config {MaxTransactionRetryTime = TimeSpan.FromSeconds(3)});
            }

            protected override void Dispose(bool isDisposing)
            {
                if (!isDisposing)
                    return;

                Driver = _baseDriver;
                base.Dispose(true);
            }

            public async Task<bool> AddItemAsync()
            {
                var session = Driver.AsyncSession();
                try
                {
                    return await session.WriteTransactionAsync(
                        async tx =>
                        {
                            await tx.RunAsync("CREATE (a:Item)");
                            return true;
                        }
                    );
                }
                catch (ServiceUnavailableException)
                {
                    return false;
                }
                finally
                {
                    await session.CloseAsync();
                }
            }

            [RequireServerFact]
            public async Task TestServiceUnavailableExample()
            {
                bool result = await AddItemAsync();

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
                var session = Driver.AsyncSession();
                try
                {
                    await session.RunAsync("CREATE (a:Person {name: $name})", new {name});
                }
                finally
                {
                    await session.CloseAsync();
                }
            }

            [RequireServerFact]
            public async Task TestSessionExample()
            {
                // Given & When
                await AddPersonAsync("Alice");
                // Then
                int count = await CountPersonAsync("Alice");
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
                var session = Driver.AsyncSession();
                try
                {
                    await session.WriteTransactionAsync(
                        tx => tx.RunAsync("CREATE (a:Person {name: $name})", new {name}));
                }
                finally
                {
                    await session.CloseAsync();
                }
            }

            [RequireServerFact]
            public async void TestTransactionFunctionExample()
            {
                // Given & When
                await AddPersonAsync("Alice");
                // Then
                int count = await CountPersonAsync("Alice");

                count.Should().Be(1);
            }
        }
    }

    [Collection(SAIntegrationCollection.CollectionName)]
    public abstract class BaseAsyncExample : IDisposable
    {
        protected ITestOutputHelper Output { get; }
        protected IDriver Driver { set; get; }
        protected const string Uri = Neo4jDefaultInstallation.BoltUri;
        protected const string User = Neo4jDefaultInstallation.User;
        protected const string Password = Neo4jDefaultInstallation.Password;

        protected BaseAsyncExample(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
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
                session.Run("MATCH (n) DETACH DELETE n").Summary();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected async Task<int> CountPersonAsync(string name)
        {
            var session = Driver.AsyncSession();
            try
            {
                return await session.ReadTransactionAsync(async tx =>
                {
                    IStatementResultCursor result =
                        await tx.RunAsync("MATCH (a:Person {name: $name}) RETURN count(a)", new {name});

                    return (await result.SingleAsync())[0].As<int>();
                });
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        protected async Task WriteAsync(string statement, object parameters)
        {
            var session = Driver.AsyncSession();
            try
            {
                await session.WriteTransactionAsync(tx => tx.RunAsync(statement, parameters));
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        protected async Task WriteAsync(string statement, IDictionary<string, object> parameters = null)
        {
            var session = Driver.AsyncSession();
            try
            {
                await session.WriteTransactionAsync(tx => tx.RunAsync(statement, parameters));
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        protected async Task<List<IRecord>> ReadAsync(string statement,
            IDictionary<string, object> parameters = null)
        {
            var session = Driver.AsyncSession();
            try
            {
                return await session.ReadTransactionAsync(
                    async tx =>
                    {
                        var cursor = await tx.RunAsync(statement, parameters);
                        return await cursor.ToListAsync();
                    });
            }
            finally
            {
                await session.CloseAsync();
            }
        }
    }

    // TODO Remove it after we figure out a way to solve the naming problem
    internal static class ValueExtensions
    {
        public static T As<T>(this object value)
        {
            return Neo4j.Driver.ValueExtensions.As<T>(value);
        }
    }
}