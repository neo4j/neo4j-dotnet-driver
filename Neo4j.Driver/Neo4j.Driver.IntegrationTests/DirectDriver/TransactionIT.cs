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
using System.Diagnostics;
using System.IO;
using System.Linq;
using FluentAssertions;
using Neo4j.Driver.V1;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests
{
    public class TransactionIT : DirectDriverIT
    {
        private IDriver Driver => Server.Driver;

        public TransactionIT(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture) : base(output, fixture)
        {
        }

        [RequireServerFact]
        public void ShouldRetry()
        {
            using (var session = Driver.Session())
            {
                var timer = new Stopwatch();
                timer.Start();
                var e = Record.Exception(() => session.WriteTransaction(tx =>
                {
                    throw new SessionExpiredException($"Failed at {timer.Elapsed}");
                }));
                timer.Stop();

                var error = e.InnerException as AggregateException;
                var innerErrors = error.Flatten().InnerExceptions;
                foreach (var innerError in innerErrors)
                {
                    Output.WriteLine(innerError.Message);
                    innerError.Should().BeOfType<SessionExpiredException>();
                }
                innerErrors.Count.Should().BeGreaterOrEqualTo(5);
                timer.Elapsed.TotalSeconds.Should().BeGreaterOrEqualTo(30);
            }
        }

        [RequireServerFact]
        public void ShouldCommitTransactionByDefault()
        {
            using (var session = Driver.Session())
            {
                var createResult = session.WriteTransaction(tx =>
                {
                    var result = tx.Run("CREATE (n) RETURN count(n)");
                    return result.Single()[0].ValueAs<int>();
                });

                // the read operation should see the commited write tx
                var matchResult = session.ReadTransaction(tx =>
                {
                    var result = tx.Run("MATCH (n) RETURN count(n)");
                    return result.Single()[0].ValueAs<int>();
                });

                createResult.Should().Be(matchResult);
            }
        }

        [RequireServerFact]
        public void ShouldNotCommitTransaction()
        {
            using (var session = Driver.Session())
            {
                var createResult = session.WriteTransaction(tx =>
                {
                    var result = tx.Run("CREATE (n) RETURN count(n)");
                    var created = result.Single()[0].ValueAs<int>();
                    tx.Failure();
                    return created;
                });

                // the read operation should not see the commited write tx
                var matchResult = session.ReadTransaction(tx =>
                {
                    var result = tx.Run("MATCH (n) RETURN count(n)");
                    return result.Single()[0].ValueAs<int>();
                });

                createResult.Should().Be(matchResult + 1);
            }
        }

        [RequireServerFact]
        public void ShouldNotCommitIfError()
        {
            using (var session = Driver.Session())
            {
                Record.Exception(()=>session.WriteTransaction(tx =>
                {
                    tx.Run("CREATE (n) RETURN count(n)");
                    tx.Success();
                    throw new ProtocolException("Broken");
                })).Should().NotBeNull();

                // the read operation should not see the commited write tx
                var matchResult = session.ReadTransaction(tx =>
                {
                    var result = tx.Run("MATCH (n) RETURN count(n)");
                    return result.Single()[0].ValueAs<int>();
                });
                matchResult.Should().Be(0);
            }
        }
    }
}