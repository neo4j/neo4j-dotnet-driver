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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.Internal;
using Neo4j.Driver;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests.DirectDriver
{
    public class CypherParametersIT : DirectDriverTestBase
    {
        private IDriver Driver => Server.Driver;

        public CypherParametersIT(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
            : base(output, fixture)
        {
        }

        [RequireServerFact]
        public async Task ShouldHandleStringLiteral()
        {
            var session = Driver.Session();
            try
            {
                var cursor = await session.RunAsync("CREATE (n:Person { name: 'Johan' })");
                var summary = await cursor.ConsumeAsync();
                summary.Counters.NodesCreated.Should().Be(1);

                cursor = await session.RunAsync("MATCH (n:Person) WHERE n.name = $name RETURN n", new {name = "Johan"});
                var list = await cursor.ToListAsync(x => x["n"].As<INode>());
                list.Should().HaveCount(1);

                var node = list.First();
                node.Should().NotBeNull();
                node.Labels.Should().Contain("Person");
                node.Properties.Should().HaveCount(1);
                node.Properties.Should().Contain(new KeyValuePair<string, object>("name", "Johan"));
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        [RequireServerFact]
        public async Task ShouldHandleRegularExpression()
        {
            var session = Driver.Session();
            try
            {
                var cursor = await session.RunAsync("CREATE (n:Person { name: 'Johan' })");
                var summary = await cursor.ConsumeAsync();
                summary.Counters.NodesCreated.Should().Be(1);

                cursor = await session.RunAsync("MATCH (n:Person) WHERE n.name =~ $regex RETURN n.name",
                    new {regex = ".*h.*"});
                var list = await cursor.ToListAsync(r => r[0].As<string>());
                list.Should().BeEquivalentTo("Johan");
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        [RequireServerFact]
        public async Task ShouldHandleCaseSensitiveStringPatternMatching()
        {
            var session = Driver.Session();
            try
            {
                var cursor = await session.RunAsync("CREATE (n:Person { name: 'Michael' })");
                var summary = await cursor.ConsumeAsync();
                summary.Counters.NodesCreated.Should().Be(1);

                cursor = await session.RunAsync("CREATE (n:Person { name: 'michael' })");
                summary = await cursor.ConsumeAsync();
                summary.Counters.NodesCreated.Should().Be(1);

                cursor = await session.RunAsync("MATCH (n:Person) WHERE n.name STARTS WITH $name RETURN n.name",
                    new {name = "Michael"});
                var list = await cursor.ToListAsync(r => r[0].As<string>());
                list.Should().BeEquivalentTo("Michael");
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        [RequireServerFact]
        public async Task ShouldHandleCreateNodeWithProperties()
        {
            var session = Driver.Session();
            try
            {
                var cursor = await session.RunAsync("CREATE ($props)",
                    new {props = new {name = "Andres", position = "Developer"}});
                var summary = await cursor.ConsumeAsync();
                summary.Counters.NodesCreated.Should().Be(1);

                cursor = await session.RunAsync("MATCH (n) WHERE n.position = 'Developer' RETURN n.name");
                var list = await cursor.ToListAsync(r => r[0].As<string>());
                list.Should().BeEquivalentTo("Andres");
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        [RequireServerFact]
        public async Task ShouldHandleCreateMultipleNodesWithProperties()
        {
            var session = Driver.Session();
            try
            {
                var cursor = await session.RunAsync("UNWIND $props AS properties CREATE(n:Person) SET n = properties",
                    new
                    {
                        props = new object[]
                        {
                            new {awesome = true, name = "Andres", position = "Developer"},
                            new {children = 3, name = "Michael", position = "Developer"}
                        }
                    });
                var summary = await cursor.ConsumeAsync();
                summary.Counters.NodesCreated.Should().Be(2);

                cursor = await session.RunAsync("MATCH (n) WHERE n.position = 'Developer' RETURN n.name");
                var list = await cursor.ToListAsync(r => r[0].As<string>());
                list.Should().BeEquivalentTo("Andres", "Michael");
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        [RequireServerFact]
        public async Task ShouldHandleSettingAllPropertiesOnANode()
        {
            var session = Driver.Session();
            try
            {
                var cursor = await session.RunAsync("CREATE (n:Person { name: 'Michaela' })");
                var summary = await cursor.ConsumeAsync();
                summary.Counters.NodesCreated.Should().Be(1);

                cursor = await session.RunAsync("MATCH (n:Person) WHERE n.name = 'Michaela' SET n = $props",
                    new {props = new {name = "Andres", position = "Developer"}});
                summary = await cursor.ConsumeAsync();
                summary.Counters.PropertiesSet.Should().Be(2);

                cursor = await session.RunAsync("MATCH (n:Person) WHERE n.name = $name RETURN n.position",
                    new {name = "Andres"});
                var list = await cursor.ToListAsync(r => r[0].As<string>());
                list.Should().BeEquivalentTo("Developer");
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        [RequireServerFact]
        public async Task ShouldHandleSkipAndLimit()
        {
            var session = Driver.Session();
            try
            {
                var cursor = await session.RunAsync(
                    "UNWIND range(1,1000) as number RETURN number SKIP $skip LIMIT $limit",
                    new {skip = 100, limit = 1});

                var list = await cursor.ToListAsync(r => r[0].As<long>());
                list.Should().AllBeEquivalentTo(101);
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        [RequireServerFact]
        public async Task ShouldHandleNodeId()
        {
            var session = Driver.Session();
            try
            {
                var cursor = await session.RunAsync("CREATE (n:Person { name: 'Michaela' }) RETURN id(n)");
                var id = await cursor.SingleAsync(r => r[0].As<int>());
                var summary = await cursor.SummaryAsync();
                summary.Counters.NodesCreated.Should().Be(1);

                cursor = await session.RunAsync("MATCH (n) WHERE id(n) = $id RETURN n.name", new {id});
                var list = await cursor.ToListAsync(r => r[0].As<string>());
                list.Should().BeEquivalentTo("Michaela");
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        [RequireServerFact]
        public async Task ShouldHandleMultipleNodeIds()
        {
            var session = Driver.Session();
            try
            {
                var cursor = await session.RunAsync(
                    "UNWIND $props AS properties CREATE(n:Person) SET n = properties RETURN id(n)",
                    new
                    {
                        props = new List<object>()
                        {
                            new {name = "Johan"},
                            new {name = "Michaela"},
                            new {name = "Andres"}
                        }
                    });
                var ids = await cursor.ToListAsync(r => r[0].As<long>());
                var summary = await cursor.SummaryAsync();
                summary.Counters.NodesCreated.Should().Be(3);

                cursor = await session.RunAsync("MATCH (n) WHERE id(n) IN $idList RETURN n.name", new {idList = ids});
                var list = await cursor.ToListAsync(r => r[0].As<string>());
                list.Should().BeEquivalentTo("Johan", "Michaela", "Andres");
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        [RequireServerFact]
        public async Task ShouldHandleCallingProcedures()
        {
            var session = Driver.Session();
            try
            {
                var cursor = await session.RunAsync("CALL dbms.queryJmx($query) yield name", new {query = "*:*"});
                var names = await cursor.ToListAsync(r => r[0].As<string>());

                names.Should().HaveCount(c => c > 0);
            }
            finally
            {
                await session.CloseAsync();
            }
        }
    }
}