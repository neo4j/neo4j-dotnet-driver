// Copyright (c) 2002-2017 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
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
using System.Linq;
using FluentAssertions;
//tag::example-import[]
using Neo4j.Driver.V1;
//end::example-import[]
using Neo4j.Driver.IntegrationTests;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.Examples
{
    /// <summary>
    /// The driver examples for 1.2 driver
    /// </summary>
    public class Examples
    {
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
                    new Config {ConnectionTimeout = TimeSpan.FromSeconds(15)});
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
                    new Config {MaxTransactionRetryTime = TimeSpan.FromSeconds(15)});
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
                    new Config {TrustStrategy = TrustStrategy.TrustSystemCaSignedCertificates});
            }
            // end::config-trust[]

            [RequireServerFact(Skip = "Requires server certificate to be installed on host system.")]
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
            public IDriver CreateDriverWithCustomizedTrustStrategy(string uri, string user, string password)
            {
                return GraphDatabase.Driver(uri, AuthTokens.Basic(user, password),
                    new Config {EncryptionLevel = EncryptionLevel.None});
            }
            // end::config-unencrypted[]

            [RequireServerFact]
            public void TestConfigUnencryptedExample()
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

        public class CustomAuthExample : BaseExample
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
                    new Config {EncryptionLevel = EncryptionLevel.None});
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
                var example = new HelloWorldExample(Uri, User, Password);
                // When & Then
                example.PrintGreeting("Hello, world");
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
                    var greater = new HelloWorldExample("bolt://localhost:7687", "neo4j", "password");
                    greater.PrintGreeting("hello, world");
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

            private static void CreatePersonNode(ITransaction tx, string name)
            {
                tx.Run("CREATE (a:Person {name: $name})", new {name});
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

        public class ServiceUnavailableExample : BaseExample
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
                catch (AggregateException)
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
    }

    [Collection(SAIntegrationCollection.CollectionName)]
    public abstract class BaseExample : IDisposable
    {
        protected ITestOutputHelper Output { get; }
        protected IDriver Driver { set; get; }
        protected const string Uri = "bolt://localhost:7687";
        protected const string User = "neo4j";
        protected const string Password = "neo4j";

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

        protected int CountPerson(string name)
        {
            using (var session = Driver.Session())
            {
                return session.ReadTransaction(
                    tx => tx.Run("MATCH (a:Person {name: $name}) RETURN count(a)",
                    new { name }).Single()[0].As<int>());
            }
        }

        protected void Write(string statement, IDictionary<string, object> parameters = null)
        {
            using (var session = Driver.Session())
            {
                session.WriteTransaction(tx =>
                    tx.Run(statement, parameters));
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
