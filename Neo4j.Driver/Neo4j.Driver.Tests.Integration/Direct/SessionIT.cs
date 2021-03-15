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
using System.Net.Sockets;
using System.Threading.Tasks;
using Neo4j.Driver.IntegrationTests.Internals;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests.Direct
{
    public class SessionIT : DirectDriverTestBase
    {
        private IDriver Driver => Server.Driver;

        public SessionIT(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture) : base(output, fixture)
        {
        }

        [RequireServerFact]
        public async Task ServiceUnavailableErrorWhenFailedToConn()
        {
            var uri = Neo4jDefaultInstallation.BoltUri.Replace(Neo4jDefaultInstallation.BoltPort, "123");

			using var driver = GraphDatabase.Driver(uri);

			var session = driver.AsyncSession();
			await using (session.ConfigureAwait(false))
			{
				var exc = await Record.ExceptionAsync(() => session.RunAsync("RETURN 1"));

				exc.Should().BeOfType<ServiceUnavailableException>();
				exc.Message.Should().Contain("Connection with the server breaks");
				exc.GetBaseException().Should().BeAssignableTo<SocketException>();
			}
        }

        [RequireServerFact]
        public async Task DisallowNewSessionAfterDriverDispose()
        {
			var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken);
			using (driver)
			{
				var session = driver.AsyncSession();
				await using (session.ConfigureAwait(false))
				{
					await VerifyRunsQuery(session);
				}
			}
            
            // Then
            var error = Record.Exception(() => driver.AsyncSession());
            error.Should().BeOfType<ObjectDisposedException>().Which
                .Message.Should().Contain("Cannot open a new session on a driver that is already disposed.");
        }

        [RequireServerFact]
        public async Task DisallowRunInSessionAfterDriverDispose()
        {
			var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken);

			var session = driver.AsyncSession();
			await VerifyRunsQuery(session);

			driver.Dispose();

			var error = await Record.ExceptionAsync(() => session.RunAsync("RETURN 1"));
            error.Should().BeOfType<ObjectDisposedException>().Which
                .Message.Should()
                .StartWith("Failed to acquire a new connection as the driver has already been disposed.");

			await session.CloseAsync();
        }

        [RequireServerFact]
        public async Task ShouldConnectAndRun()
        {
            var session = Driver.AsyncSession();
			await using (session.ConfigureAwait(false))
			{
				var result = await session.RunAsync("RETURN 2 as Number");
				await result.ConsumeAsync();

				var keys = await result.KeysAsync();
				keys.Should().BeEquivalentTo("Number");
			}
        }

        [RequireServerFact]
        public async Task ShouldBeAbleToRunMultiQuerysInOneTransaction()
        {
            var session = Driver.AsyncSession();
			await using (session.ConfigureAwait(false))
			{
				var tx = await session.BeginTransactionAsync();
				try
				{
					// clean db
					await tx.RunAndConsumeAsync("MATCH (n) DETACH DELETE n RETURN count(*)");

					var record = await tx.RunAndSingleAsync("CREATE (n {name:'Steve Brook'}) RETURN n.name", null);

					record["n.name"].Should().Be("Steve Brook");

					await tx.CommitAsync();
				}
				catch
				{
					await tx.RollbackAsync();
					throw;
				}
			}
        }

        [RequireServerFact]
        public async Task TheSessionErrorShouldBeClearedForEachSession()
        {
            var session = Driver.AsyncSession();
			await using (session.ConfigureAwait(false))
			{
				var ex = await Record.ExceptionAsync(() => session.RunAndConsumeAsync("Invalid Cypher"));

				ex.Should().BeOfType<ClientException>().Which
					.Message.Should().StartWith("Invalid input");
			}
            

            session = Driver.AsyncSession();
			await using (session.ConfigureAwait(false))
			{
				await VerifyRunsQuery(session);
			}
        }

        [RequireServerFact]
        public async Task AfterErrorTheFirstSyncShouldAckFailureSoThatNewQueryCouldRun()
        {
            var session = Driver.AsyncSession();
			await using (session.ConfigureAwait(false))
			{
				var ex = await Record.ExceptionAsync(() => session.RunAndConsumeAsync("Invalid Cypher"));
				ex.Should().BeOfType<ClientException>().Which
					.Message.Should().StartWith("Invalid input");

				var result = await session.RunAndSingleAsync("RETURN 1", null);
				result[0].Should().BeEquivalentTo(1);
			}
        }

        [RequireServerFact]
        public async Task RollBackTxIfErrorWithConsume()
        {
            // Given
            var session = Driver.AsyncSession();
			await using (session.ConfigureAwait(false))
			{
				// When failed to run a tx with consume
				var tx = await session.BeginTransactionAsync();
				try
				{
					var ex = await Record.ExceptionAsync(() => tx.RunAndConsumeAsync("Invalid Cypher"));
					ex.Should().BeOfType<ClientException>().Which
						.Message.Should().StartWith("Invalid input");
				}
				finally
				{
					await tx.RollbackAsync();
				}

				// Then can run more afterwards
				var result = await session.RunAndSingleAsync("RETURN 1", null);
				result[0].Should().BeEquivalentTo(1);
			}           
        }

        [RequireServerFact]
        public async Task RollBackTxIfErrorWithoutConsume()
        {
            // Given
            var session = Driver.AsyncSession();
			await using (session.ConfigureAwait(false))
			{
                // When failed to run a tx without consume
                var tx = await session.BeginTransactionAsync();
                await tx.RunAsync("CREATE (a { name: 'lizhen' })");
                await tx.RunAsync("Invalid Cypher");

                var ex = await Record.ExceptionAsync(() => tx.CommitAsync());
                ex.Should().BeOfType<ClientException>().Which
                    .Message.Should().StartWith("Invalid input");

                // Then can still run more afterwards
                var anotherTx = await session.BeginTransactionAsync();
                try
                {
                    var result =
                        await anotherTx.RunAndSingleAsync("MATCH (a {name : 'a person'}) RETURN count(a)", null);
                    result[0].Should().BeEquivalentTo(0);

                    await anotherTx.CommitAsync();
                }
                catch
                {
                    await anotherTx.RollbackAsync();
                    throw;
                }
            }
        }

        [RequireServerFact]
        public async Task ShouldNotThrowExceptionWhenSessionClosedAfterDriver()
        {
            using var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken);

            var session = driver.AsyncSession();
			await using (session.ConfigureAwait(false))
			{
				var tx = await session.BeginTransactionAsync();
				try
				{
					var ex = await Record.ExceptionAsync(() => tx.RunAndConsumeAsync("Invalid Cypher"));
					ex.Should().BeOfType<ClientException>().Which
						.Message.Should().StartWith("Invalid input");
				}
				finally
				{
					await tx.RollbackAsync();
				}

				var result = await session.RunAndSingleAsync("RETURN 1", null);
				result[0].Should().BeEquivalentTo(1);
			}            
        }

        [RequireServerFact]
        public async Task KeysShouldBeAvailableAfterRun()
        {
			using var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken);

            var session = driver.AsyncSession();
			await using (session.ConfigureAwait(false))
			{
                var cursor = await session.RunAsync("RETURN 1 As X");
                var keys = await cursor.KeysAsync();

                keys.Should().BeEquivalentTo("X");
            }
        }

        [RequireServerFact]
        public async Task KeysShouldBeAvailableAfterRunAndResultConsumption()
        {
			using var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken);

            var session = driver.AsyncSession();
			await using (session.ConfigureAwait(false))
			{
                var cursor = await session.RunAsync("RETURN 1 As X");
                var keys1 = await cursor.KeysAsync();

                keys1.Should().BeEquivalentTo("X");

                await cursor.ConsumeAsync();

                var keys2 = await cursor.KeysAsync();
                keys2.Should().BeEquivalentTo("X");
            }
        }

        [RequireServerFact]
        public async Task KeysShouldBeAvailableAfterConsecutiveRun()
        {
			using var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken);
            
            var session = driver.AsyncSession();
			await using (session.ConfigureAwait(false))
			{
                var cursor1 = await session.RunAsync("RETURN 1 As X");
                var cursor2 = await session.RunAsync("RETURN 1 As Y");

                var keys1 = await cursor1.KeysAsync();
                keys1.Should().BeEquivalentTo("X");

                var keys2 = await cursor2.KeysAsync();
                keys2.Should().BeEquivalentTo("Y");
            }
        }

        [RequireServerFact]
        public async Task KeysShouldBeAvailableAfterConsecutiveRunAndResultConsumption()
        {
			using var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken);
            
            var session = driver.AsyncSession();
			await using (session.ConfigureAwait(false))
			{
                var cursor1 = await session.RunAsync("RETURN 1 As X");
                var cursor2 = await session.RunAsync("RETURN 1 As Y");

                var keys1 = await cursor1.KeysAsync();
                keys1.Should().BeEquivalentTo("X");
                var keys2 = await cursor2.KeysAsync();
                keys2.Should().BeEquivalentTo("Y");

                await cursor1.ConsumeAsync();
                await cursor2.ConsumeAsync();

                keys1 = await cursor1.KeysAsync();
                keys1.Should().BeEquivalentTo("X");
                keys2 = await cursor2.KeysAsync();
                keys2.Should().BeEquivalentTo("Y");
            }            
        }

        [RequireServerFact]
        public async Task KeysShouldBeAvailableAfterConsecutiveRunNoOrder()
        {
			using var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken);
            
			var session = driver.AsyncSession();
			await using (session.ConfigureAwait(false))
			{
                var cursor1 = await session.RunAsync("RETURN 1 As X");
                var cursor2 = await session.RunAsync("RETURN 1 As Y");

                var keys2 = await cursor2.KeysAsync();
                keys2.Should().BeEquivalentTo("Y");
                var keys1 = await cursor1.KeysAsync();
                keys1.Should().BeEquivalentTo("X");
            }
            
        }

        [RequireServerFact]
        public async Task KeysShouldBeAvailableAfterConsecutiveRunAndResultConsumptionNoOrder()
        {
			using var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken);
            
			var session = driver.AsyncSession();
			await using (session.ConfigureAwait(false))
			{
                var cursor1 = await session.RunAsync("RETURN 1 As X");
                var cursor2 = await session.RunAsync("RETURN 1 As Y");

                var keys2 = await cursor2.KeysAsync();
                keys2.Should().BeEquivalentTo("Y");
                var keys1 = await cursor1.KeysAsync();
                keys1.Should().BeEquivalentTo("X");

                await cursor2.ConsumeAsync();
                await cursor1.ConsumeAsync();

                keys2 = await cursor2.KeysAsync();
                keys2.Should().BeEquivalentTo("Y");
                keys1 = await cursor1.KeysAsync();
                keys1.Should().BeEquivalentTo("X");
            }
        }

        [RequireServerFact]
        public async Task ShouldNotBeAbleToAccessRecordsAfterSessionClose()
        {
			using var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken);

            var session = driver.AsyncSession();
            IResultCursor cursor;
			await using (session.ConfigureAwait(false))
			{
                cursor = await session.RunAsync("RETURN 1 As X");
            }
                
            var error = await Record.ExceptionAsync(async () => await cursor.ToListAsync());
            error.Should().BeOfType<ResultConsumedException>();            
        }

        [RequireServerFact]
        public async Task ShouldNotBeAbleToAccessRecordsAfterSummary()
        {
			using var driver = GraphDatabase.Driver(ServerEndPoint, AuthToken);
            
            var session = driver.AsyncSession();
			await using (session.ConfigureAwait(false))
			{
				var cursor = await session.RunAsync("RETURN 1 As X");
				await cursor.ConsumeAsync();

				var error = await Record.ExceptionAsync(async () => await cursor.ToListAsync());
				error.Should().BeOfType<ResultConsumedException>();
			}             
        }

        private static async Task VerifyRunsQuery(IAsyncSession session)
        {
            var record = await session.RunAndSingleAsync("RETURN 1 AS Number", null);

            record.Keys.Should().BeEquivalentTo("Number");
            record.Values.Should().BeEquivalentTo(new KeyValuePair<string, object>("Number", 1));
        }
    }
}