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
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Reactive.Testing;
using Neo4j.Driver.IntegrationTests.Internals;
using Neo4j.Driver.Reactive;
using Xunit;
using Xunit.Abstractions;
using static Microsoft.Reactive.Testing.ReactiveTest;
using static Neo4j.Driver.IntegrationTests.Internals.VersionComparison;

namespace Neo4j.Driver.IntegrationTests;

public class ExamplesRx
{
    public class RxSectionExamples : BaseRxExample
    {
        public RxSectionExamples(StandAloneIntegrationTestFixture fixture)
            : base(fixture)
        {
        }

        // tag::rx-autocommit-transaction[]
        public IObservable<string> ReadProductTitles()
        {
            var session = Driver.RxSession();

            return session.Run(
                    "MATCH (p:Product) WHERE p.id = $id RETURN p.title", // Cypher query
                    new { id = 0 } // Parameters in the query, if any
                )
                .Records()
                .Select(record => record[0].ToString())
                .OnErrorResumeNext(session.Close<string>());
        }
        // end::rx-autocommit-transaction[]

        // tag::rx-transaction-function[]
        public IObservable<string> PrintAllProducts()
        {
            var session = Driver.RxSession();

            return session.ExecuteRead(
                    tx =>
                    {
                        return tx.Run(
                                "MATCH (p:Product) WHERE p.id = $id RETURN p.title", // Cypher query
                                new { id = 0 } // Parameters in the query, if any
                            )
                            .Records()
                            .Select(record => record[0].ToString());
                    })
                .OnErrorResumeNext(session.Close<string>());
        }
        // end::rx-transaction-function[]

        // tag::rx-explicit-transaction[]
        public IObservable<string> PrintSingleProduct()
        {
            var session = Driver.RxSession();

            // Start an explicit transaction
            return session.BeginTransaction()
                .SelectMany(
                    tx => tx.Run(
                            "MATCH (p:Product) WHERE p.id = $id RETURN p.title", // Cypher query
                            new { id = 0 } // Parameters in the query, if any
                        )
                        .Records()
                        .Select(record => record[0].ToString())
                        .Concat(tx.Commit<string>())
                        .Catch(tx.Rollback<string>()));
        }
        // end::rx-explicit-transaction[]

        [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
        public async void TestAutocommitTransactionExample()
        {
            await WriteAsync(
                "CREATE (p:Product) SET p.id = $id, p.title = $title",
                new { id = 0, title = "Product-0" });

            var results = ReadProductTitles();

            results.WaitForCompletion()
                .AssertEqual(
                    OnNext(0, "Product-0"),
                    OnCompleted<string>(0));
        }

        [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
        public async void TestTransactionFunctionExample()
        {
            await WriteAsync(
                "CREATE (p:Product) SET p.id = $id, p.title = $title",
                new { id = 0, title = "Product-0" });

            var results = PrintAllProducts();

            results.WaitForCompletion()
                .AssertEqual(
                    OnNext(0, "Product-0"),
                    OnCompleted<string>(0));
        }

        [RequireServerFact("4.0.0", GreaterThanOrEqualTo)]
        public async void TestExplicitTransactionExample()
        {
            await WriteAsync(
                "CREATE (p:Product) SET p.id = $id, p.title = $title",
                new { id = 0, title = "Product-0" });

            var results = PrintSingleProduct();

            results.WaitForCompletion()
                .AssertEqual(
                    OnNext(0, "Product-0"),
                    OnCompleted<string>(0));
        }
    }

    public class ResultConsumeExample : BaseAsyncExample
    {
        public ResultConsumeExample(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
            : base(output, fixture)
        {
        }

        // tag::rx-result-consume[]
        public IObservable<string> GetPeople()
        {
            var session = Driver.RxSession();
            return session.ExecuteRead(
                    tx =>
                    {
                        return tx.Run("MATCH (a:Person) RETURN a.name ORDER BY a.name")
                            .Records()
                            .Select(record => record[0].As<string>());
                    })
                .OnErrorResumeNext(session.Close<string>());
        }
        // end::rx-result-consume[]

        [RequireServerFact]
        public async Task TestResultConsumeExample()
        {
            // Given
            await WriteAsync("CREATE (a:Person {name: 'Alice'})");
            await WriteAsync("CREATE (a:Person {name: 'Bob'})");
            // When & Then
            var results = GetPeople();

            results.WaitForCompletion()
                .AssertEqual(
                    OnNext(0, "Alice"),
                    OnNext(0, "Bob"),
                    OnCompleted<string>(0));
        }
    }
}

[Collection(SaIntegrationCollection.CollectionName)]
public abstract class BaseRxExample : AbstractRxTest, IDisposable
{
    private bool _disposed;
    protected string Uri = DefaultInstallation.BoltUri;
    protected string User = DefaultInstallation.User;

    protected BaseRxExample(StandAloneIntegrationTestFixture fixture)
    {
        Driver = fixture.StandAloneSharedInstance.Driver;
    }

    protected IDriver Driver { set; get; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~BaseRxExample()
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

    protected async Task WriteAsync(string query, object parameters)
    {
        await using var session = Driver.AsyncSession();
        await session.ExecuteWriteAsync(async tx => await tx.RunAsync(query, parameters));
    }
}
