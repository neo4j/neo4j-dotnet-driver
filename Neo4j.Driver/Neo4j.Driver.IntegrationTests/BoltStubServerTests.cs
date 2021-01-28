﻿// Copyright (c) "Neo4j"
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
using System.Linq;
using FluentAssertions;
using Neo4j.Driver.IntegrationTests.Internals;
using Neo4j.Driver.IntegrationTests.Shared;
using Neo4j.Driver.V1;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests
{
    public class BoltStubServerTests
    {
        public Config Config { get; set; }

        public BoltStubServerTests(ITestOutputHelper output)
        {
            Config = new Config {EncryptionLevel = EncryptionLevel.None, DriverLogger = new TestDriverLogger(output)};
        }

        [RequireBoltStubServerFact]
        public void SendRoutingContextToServer()
        {
            using (BoltStubServer.Start("get_routing_table_with_context", 9001))
            {
                var uri = new Uri("bolt+routing://127.0.0.1:9001/?policy=my_policy&region=china");
                using (var driver = GraphDatabase.Driver(uri, Config))
                using (var session = driver.Session())
                {
                    var records = session.Run("MATCH (n) RETURN n.name AS name").ToList();
                    records.Count.Should().Be(2);
                    records[0]["name"].ValueAs<string>().Should().Be("Alice");
                    records[1]["name"].ValueAs<string>().Should().Be("Bob");
                }
            }
        }

        [RequireBoltStubServerFact]
        public void ShouldSupportNeo4jScheme()
        {
            using (BoltStubServer.Start("get_routing_table_with_context", 9001))
            {
                var uri = new Uri("neo4j://127.0.0.1:9001/?policy=my_policy&region=china");
                using (var driver = GraphDatabase.Driver(uri, Config))
                using (var session = driver.Session())
                {
                    var records = session.Run("MATCH (n) RETURN n.name AS name").ToList();
                    records.Count.Should().Be(2);
                    records[0]["name"].ValueAs<string>().Should().Be("Alice");
                    records[1]["name"].ValueAs<string>().Should().Be("Bob");
                }
            }
        }

        [RequireBoltStubServerFact]
        public void ShouldLogServerAddress()
        {
            var logs = new List<string>();
            var config = new Config
            {
                EncryptionLevel = EncryptionLevel.None,
                DriverLogger = new TestDriverLogger(logs.Add, ExtendedLogLevel.Debug)
            };
            using (BoltStubServer.Start("accessmode_reader_implicit", 9001))
            {
                using (var driver = GraphDatabase.Driver("bolt://localhost:9001", AuthTokens.None, config))
                {
                    using (var session = driver.Session(AccessMode.Read))
                    {
                        var list = session.Run("RETURN $x", new {x = 1}).Select(r => Convert.ToInt32(r[0])).ToList();
                        list.Should().HaveCount(1).And.Contain(1);
                    }
                }
            }

            foreach (var log in logs)
            {
                if (log.StartsWith("[Debug]:[conn-"))
                {
                    log.Should().Contain("localhost:9001");
                }
            }
        }

        [RequireBoltStubServerFact]
        public void InvokeProcedureGetRoutingTableWhenServerVersionPermits()
        {
            using (BoltStubServer.Start("get_routing_table", 9001))
            {
                var uri = new Uri("bolt+routing://127.0.0.1:9001");
                using (var driver = GraphDatabase.Driver(uri, Config))
                using (var session = driver.Session())
                {
                    var records = session.Run("MATCH (n) RETURN n.name AS name").ToList();
                    records.Count.Should().Be(3);
                    records[0]["name"].ValueAs<string>().Should().Be("Alice");
                    records[1]["name"].ValueAs<string>().Should().Be("Bob");
                    records[2]["name"].ValueAs<string>().Should().Be("Eve");
                }
            }
        }

        [RequireBoltStubServerFact]
        public void CanSendMultipleBookmarks()
        {
            var bookmarks = new[]
            {
                "neo4j:bookmark:v1:tx5", "neo4j:bookmark:v1:tx29",
                "neo4j:bookmark:v1:tx94", "neo4j:bookmark:v1:tx56",
                "neo4j:bookmark:v1:tx16", "neo4j:bookmark:v1:tx68"
            };
            using (BoltStubServer.Start("multiple_bookmarks", 9001))
            {
                var uri = new Uri("bolt://127.0.0.1:9001");
                using (var driver = GraphDatabase.Driver(uri, Config))
                using (var session = driver.Session(bookmarks))
                {
                    using (var tx = session.BeginTransaction())
                    {
                        tx.Run("CREATE (n {name:'Bob'})");
                        tx.Success();
                    }
                    session.LastBookmark.Should().Be("neo4j:bookmark:v1:tx95");
                }
            }
        }

        [RequireBoltStubServerFact]
        public void ShouldOnlyResetAfterError()
        {
            using (BoltStubServer.Start("rollback_error", 9001))
            {
                var uri = new Uri("bolt://127.0.0.1:9001");
                using (var driver = GraphDatabase.Driver(uri, Config))
                using (var session = driver.Session())
                {
                    var tx = session.BeginTransaction();
                    var result = tx.Run("CREATE (n {name:'Alice'}) RETURN n.name AS name");
                    var exception = Record.Exception(() => result.Consume());
                    exception.Should().BeOfType<TransientException>();
                    tx.Dispose();
                }
            }
        }
    }
}
