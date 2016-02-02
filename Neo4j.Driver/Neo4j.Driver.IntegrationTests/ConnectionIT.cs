//  Copyright (c) 2002-2016 "Neo Technology,"
//  Network Engine for Objects in Lund AB [http://neotechnology.com]
// 
//  This file is part of Neo4j.
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
using System;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests
{
    [Collection(IntegrationCollection.CollectionName)]
    public class ConnectionIT
    {
        private int Port { get; set; }
        private string ServerEndPoint => $"bolt://localhost:{Port}";


        private readonly ITestOutputHelper output;

        public ConnectionIT(ITestOutputHelper output, IntegrationTestFixture fixture)
        {
            this.output = output;
            Port = fixture.Port;
        }

        [Fact]
        public void ShouldDoHandShake()
        {
            using (var driver = GraphDatabase.Driver(
                ServerEndPoint, 
                Config.Builder.WithLogger( new DebugLogger {Level = LogLevel.Trace}).ToConfig()))
            {
                using (var session = driver.Session())
                {
                    var resultCursor = session.Run("RETURN 2 as Number" );
                    resultCursor.Keys.Should().Contain("Number");
                    resultCursor.Keys.Count.Should().Be(1);
                 //   resultCursor.Stream.Count.Should().Be(1);
//                    var record = resultCursor.Stream.First();
//                    Assert.Equal(2, record.Values["Number"]);
//                    Assert.IsType<sbyte>(record.Values["Number"]);
                }
            }
        }

        [Fact]
        public void ShouldBeAbleToRunMultiStatementsInOneTransaction()
        {
            using (var driver = GraphDatabase.Driver("bolt://localhost:7687"))
            using (var session = driver.Session())
            using (var tx = session.BeginTransaction())
            {
                // clean db
                tx.Run("MATCH (n) DETACH DELETE n RETURN count(*)");
                var result = tx.Run("CREATE (n {name:'Steve Brook'}) RETURN n.name");

                while (result.Next())
                {
                    foreach (var keyValuePair in result.Values())
                    {
                        output.WriteLine($"{keyValuePair.Key} = {keyValuePair.Value}");
                    }
                }
            }
        }
    }
}