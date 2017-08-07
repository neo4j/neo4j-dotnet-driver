using FluentAssertions;
using Neo4j.Driver.V1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neo4j.Driver.IntegrationTests;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.ExamplesAsync
{
    public class ExamplesAsync
    {

        public class AutocommitTransactionExample : BaseAsyncExample
        {
            public AutocommitTransactionExample(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
                : base(output, fixture)
            {
            }

            // tag::autocommit-transaction[]
            public async Task AddPersonAsync(string name)
            {
                var session = Driver.Session();
                try
                {
                    await session.RunAsync("CREATE (a:Person {name: $name})", new {name});
                }
                finally
                {
                    await session.CloseAsync();
                }
            }
            // end::autocommit-transaction[]

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

            // tag::basic-auth[]
            public IDriver CreateDriverWithBasicAuth(string uri, string user, string password)
            {
                return GraphDatabase.Driver(uri, AuthTokens.Basic(user, password));
            }
            // end::basic-auth[]

            [RequireServerFact]
            public async Task TestBasicAuthExample()
            {
                // Given
                var driver = CreateDriverWithBasicAuth(Uri, User, Password);
                var session = driver.Session();
                try
                {
                    // When & Then
                    IStatementResultReader result = await session.RunAsync("RETURN 1");

                    bool read = await result.ReadAsync();
                    read.Should().BeTrue();

                    result.Current()[0].As<int>().Should().Be(1);
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

            // tag::config-connection-timeout[]
            public IDriver CreateDriverWithCustomizedConnectionTimeout(string uri, string user, string password)
            {
                return GraphDatabase.Driver(uri, AuthTokens.Basic(user, password),
                    new Config { ConnectionTimeout = TimeSpan.FromSeconds(15) });
            }
            // end::config-connection-timeout[]

            [RequireServerFact]
            public async Task TestConfigConnectionTimeoutExample()
            {
                // Given
                var driver = CreateDriverWithCustomizedConnectionTimeout(Uri, User, Password);
                var session = driver.Session();
                try
                {
                    // When & Then
                    IStatementResultReader result = await session.RunAsync("RETURN 1");

                    bool read = await result.ReadAsync();
                    read.Should().BeTrue();

                    result.Current()[0].As<int>().Should().Be(1);
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

            // tag::config-max-retry-time[]
            public IDriver CreateDriverWithCustomizedMaxRetryTime(string uri, string user, string password)
            {
                return GraphDatabase.Driver(uri, AuthTokens.Basic(user, password),
                    new Config { MaxTransactionRetryTime = TimeSpan.FromSeconds(15) });
            }
            // end::config-max-retry-time[]

            [RequireServerFact]
            public async Task TestConfigMaxRetryTimeExample()
            {
                // Given
                var driver = CreateDriverWithCustomizedMaxRetryTime(Uri, User, Password);
                var session = driver.Session();
                try
                {
                    // When & Then
                    IStatementResultReader result = await session.RunAsync("RETURN 1");

                    bool read = await result.ReadAsync();
                    read.Should().BeTrue();

                    result.Current()[0].As<int>().Should().Be(1);
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

            // tag::config-trust[]
            public IDriver CreateDriverWithCustomizedTrustStrategy(string uri, string user, string password)
            {
                return GraphDatabase.Driver(uri, AuthTokens.Basic(user, password),
                    new Config { TrustStrategy = TrustStrategy.TrustAllCertificates });
            }
            // end::config-trust[]

            [RequireServerFact]
            public async Task TestConfigTrustExample()
            {
                // Given
                var driver = CreateDriverWithCustomizedTrustStrategy(Uri, User, Password);
                var session = driver.Session();
                try
                {
                    // When & Then
                    IStatementResultReader result = await session.RunAsync("RETURN 1");

                    bool read = await result.ReadAsync();
                    read.Should().BeTrue();

                    result.Current()[0].As<int>().Should().Be(1);
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

            // tag::config-unencrypted[]
            public IDriver CreateDriverWithCustomizedSecurityStrategy(string uri, string user, string password)
            {
                return GraphDatabase.Driver(uri, AuthTokens.Basic(user, password),
                    new Config { EncryptionLevel = EncryptionLevel.None });
            }
            // end::config-unencrypted[]

            [RequireServerFact]
            public async Task TestConfigUnencryptedExample()
            {
                // Given
                var driver = CreateDriverWithCustomizedSecurityStrategy(Uri, User, Password);
                var session = driver.Session();
                try
                {
                    // When & Then
                    IStatementResultReader result = await session.RunAsync("RETURN 1");

                    bool read = await result.ReadAsync();
                    read.Should().BeTrue();

                    result.Current()[0].As<int>().Should().Be(1);
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

            // tag::custom-auth[]
            public IDriver CreateDriverWithCustomizedAuth(string uri,
                string principal, string credentials, string realm, string scheme, Dictionary<string, object> parameters)
            {
                return GraphDatabase.Driver(uri, AuthTokens.Custom(principal, credentials, realm, scheme, parameters),
                    new Config { EncryptionLevel = EncryptionLevel.None });
            }
            // end::custom-auth[]

            [RequireServerFact]
            public async Task TestCustomAuthExample()
            {
                // Given
                var driver = CreateDriverWithCustomizedAuth(Uri, User, Password, "native", "basic", null);
                var session = driver.Session();
                try
                {
                    // When & Then
                    IStatementResultReader result = await session.RunAsync("RETURN 1");

                    bool read = await result.ReadAsync();
                    read.Should().BeTrue();

                    result.Current()[0].As<int>().Should().Be(1);
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

            // tag::cypher-error[]
            public async Task<int> GetEmployeeNumberAsync(string name)
            {
                var session = Driver.Session();
                try
                {
                    return await session.ReadTransactionAsync(async tx => await SelectEmployee(tx, name));
                }
                finally
                {
                    await session.CloseAsync();
                }
            }

            private async Task<int> SelectEmployee(ITransactionAsync tx, string name)
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
            // end::cypher-error[]

            [RequireServerFact]
            public async Task TestCypherErrorExample()
            {
                // When & Then
                int result = await GetEmployeeNumberAsync("Alice");
                
                result.Should().Be(-1);
            }
        }

        public class DriverLifecycleExampleTest : BaseAsyncExample
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
            public async Task TestDriverLifecycleExample()
            {
                // Given
                var driver = new DriverLifecycleExample(Uri, User, Password).Driver;
                var session = driver.Session();
                try
                {
                    // When & Then
                    IStatementResultReader result = await session.RunAsync("RETURN 1");

                    bool read = await result.ReadAsync();
                    read.Should().BeTrue();

                    result.Current()[0].As<int>().Should().Be(1);
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

            // tag::hello-world[]
            public class HelloWorldExample : IDisposable
            {
                private readonly IDriver _driver;

                public HelloWorldExample(string uri, string user, string password)
                {
                    _driver = GraphDatabase.Driver(uri, AuthTokens.Basic(user, password));
                }

                public async Task PrintGreetingAsync(string message)
                {
                    var session = _driver.Session();
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

                public static void Main()
                {
                    using (var greeter = new HelloWorldExample("bolt://localhost:7687", "neo4j", "password"))
                    {
                        greeter.PrintGreetingAsync("hello, world").Wait();
                    }
                }
            }
            // end::hello-world[]
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

            // tag::read-write-transaction[]
            public async Task<long> AddPersonAsync(string name)
            {
                var session = Driver.Session();
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

            private static Task CreatePersonNodeAsync(ITransactionAsync tx, string name)
            {
                return tx.RunAsync("CREATE (a:Person {name: $name})", new { name });
            }

            private static async Task<long> MatchPersonNodeAsync(ITransactionAsync tx, string name)
            {
                var result = await tx.RunAsync("MATCH (a:Person {name: $name}) RETURN id(a)", new { name });

                
                return (await result.SingleAsync())[0].As<long>();
            }
            // end::read-write-transaction[]
        }

        public class ResultConsumeExample : BaseAsyncExample
        {
            public ResultConsumeExample(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
                : base(output, fixture)
            {
            }

            // tag::result-consume[]
            public async Task<List<string>> GetPeopleAsync()
            {
                var session = Driver.Session();
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
            // end::result-consume[]

            [RequireServerFact]
            public async Task TestResultConsumeExample()
            {
                // Given
                await WriteAsync("CREATE (a:Person {name: 'Alice'})");
                await WriteAsync("CREATE (a:Person {name: 'Bob'})");
                // When & Then
                List<string> people = await GetPeopleAsync();

                people.Should().Contain(new[] { "Alice", "Bob" });
            }
        }

        public class ResultRetainExample : BaseAsyncExample
        {
            public ResultRetainExample(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
                : base(output, fixture)
            {
            }

            // tag::result-retain[]
            public async Task<int> AddEmployeesAsync(string companyName)
            {
                var session = Driver.Session();
                try
                {
                    var persons = await session.ReadTransactionAsync(async tx =>
                    {
                        IStatementResultReader result = await tx.RunAsync("MATCH (a:Person) RETURN a.name AS name");
                        return await result.ToListAsync();
                    });

                    return persons.Sum(person => session.WriteTransactionAsync(async tx =>
                    {
                        await tx.RunAsync("MATCH (emp:Person {name: $person_name}) " +
                                          "MERGE (com:Company {name: $company_name}) " +
                                          "MERGE (emp)-[:WORKS_FOR]->(com)",
                            new {person_name = person["name"].As<string>(), company_name = companyName});

                        return 1;
                    }).Result);
                }
                finally
                {
                    await session.CloseAsync();
                }
            }
            // end::result-retain[]

            [RequireServerFact]
            public async Task TestResultConsumeExample()
            {
                // Given
                await WriteAsync("CREATE (a:Person {name: 'Alice'})");
                await WriteAsync("CREATE (a:Person {name: 'Bob'})");
                // When & Then
                int count = await AddEmployeesAsync("Acme");
                count.Should().Be(2);

                var result = await ReadAsync("MATCH (emp:Person)-[WORKS_FOR]->(com:Company) WHERE com.name = 'Acme' RETURN count(emp)");

                bool next = await result.ReadAsync();
                next.Should().BeTrue();

                result.Current()[0].As<int>().Should().Be(2);
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
                    new Config { MaxTransactionRetryTime = TimeSpan.FromSeconds(3) });
            }

            protected override void Dispose(bool isDisposing)
            {
                if (!isDisposing)
                    return;

                Driver = _baseDriver;
                base.Dispose(true);
            }

            // tag::service-unavailable[]
            public async Task<bool> AddItemAsync()
            {
                var session = Driver.Session();
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
                catch (AggregateException)
                {
                    return false;
                }
                finally
                {
                    await session.CloseAsync();
                }
            }
            // end::service-unavailable[]

            [RequireServerFact]
            public async Task TestServiceUnavailableExample()
            {
                bool result = await AddItemAsync();
                
                result.Should().BeFalse();
            }
        }

        public class SessionExample : BaseAsyncExample
        {
            public SessionExample(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
                : base(output, fixture)
            {
            }

            // tag::session[]
            public async Task AddPersonAsync(string name)
            {
                var session = Driver.Session();
                try
                {
                    await session.RunAsync("CREATE (a:Person {name: $name})", new {name});
                }
                finally
                {
                    await session.CloseAsync();
                }
            }
            // end::session[]

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


        public class TransactionFunctionExample : BaseAsyncExample
        {
            public TransactionFunctionExample(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
                : base(output, fixture)
            {
            }

            // tag::transaction-function[]
            public async Task AddPersonAsync(string name)
            {
                var session = Driver.Session();
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
            // end::transaction-function[]

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
        protected const string Uri = "bolt://localhost:7687";
        protected const string User = "neo4j";
        protected const string Password = "neo4j";

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
                var result = session.Run("MATCH (n) DETACH DELETE n");
                result.Consume();

            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected async Task<int> CountPersonAsync(string name)
        {
            var session = Driver.Session();
            try
            {
                return await session.ReadTransactionAsync(async tx =>
                {
                    IStatementResultReader result =
                        await tx.RunAsync("MATCH (a:Person {name: $name}) RETURN count(a)", new {name});

                    return (await result.SingleAsync())[0].As<int>();
                });
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        protected async Task WriteAsync(string statement, IDictionary<string, object> parameters = null)
        {
            var session = Driver.Session();
            try
            {
                await session.WriteTransactionAsync(tx => tx.RunAsync(statement, parameters));
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        protected async Task<IStatementResultReader> ReadAsync(string statement, IDictionary<string, object> parameters = null)
        {
            var session = Driver.Session();
            try
            {
                return await session.ReadTransactionAsync(tx => tx.RunAsync(statement, parameters));
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
            return V1.ValueExtensions.As<T>(value);
        }
    }
}

