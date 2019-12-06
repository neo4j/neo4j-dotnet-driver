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
using Neo4j.Driver.V1;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests.DirectDriver
{
    public class CypherParametersIT: DirectDriverTestBase
    {
        private IDriver Driver => Server.Driver;

        public CypherParametersIT(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture) 
            : base(output, fixture)
        {

        }

        [RequireServerFact]
        public void ShouldHandleStringLiteral()
        {
            using (var session = Driver.Session())
            {
                var result = session.Run("CREATE (n:Person { name: 'Johan' })");
                result.Summary.Counters.NodesCreated.Should().Be(1);

                result = session.Run("MATCH (n:Person) WHERE n.name = $name RETURN n", new {name = "Johan"});
                var list = result.Select(r => r).ToList();
                list.Should().HaveCount(1);

                var node = (INode) list.First()[0];
                node.Should().NotBeNull();
                node.Labels.Should().Contain("Person");
                node.Properties.Should().HaveCount(1);
                node.Properties.Should().Contain(new KeyValuePair<string, object>("name", "Johan"));
            }
        }

        [RequireServerFact]
        public void ShouldHandleRegularExpression()
        {
            using (var session = Driver.Session())
            {
                var result = session.Run("CREATE (n:Person { name: 'Johan' })");
                result.Summary.Counters.NodesCreated.Should().Be(1);

                result = session.Run("MATCH (n:Person) WHERE n.name =~ $regex RETURN n.name", new {regex = ".*h.*"});
                var list = result.Select(r => r[0]).ToList();
                list.Should().HaveCount(1);
                list.Should().Contain("Johan");
            }
        }

        [RequireServerFact]
        public void ShouldHandleCaseSensitiveStringPatternMatching()
        {
            using (var session = Driver.Session())
            {
                var result = session.Run("CREATE (n:Person { name: 'Michael' })");
                result.Summary.Counters.NodesCreated.Should().Be(1);

                result = session.Run("CREATE (n:Person { name: 'michael' })");
                result.Summary.Counters.NodesCreated.Should().Be(1);

                result = session.Run("MATCH (n:Person) WHERE n.name STARTS WITH $name RETURN n.name", new { name = "Michael" });
                var list = result.Select(r => r[0]).ToList();
                list.Should().HaveCount(1);
                list.Should().Contain("Michael");
            }
        }

        [RequireServerFact]
        public void ShouldHandleCreateNodeWithProperties()
        {
            using (var session = Driver.Session())
            {
                var result = session.Run("CREATE ($props)", new { props = new { name = "Andres", position = "Developer" } });
                result.Summary.Counters.NodesCreated.Should().Be(1);

                result = session.Run("MATCH (n) WHERE n.position = 'Developer' RETURN n.name");
                var list = result.Select(r => r[0]).ToList();
                list.Should().HaveCount(1);
                list.Should().Contain("Andres");
            }
        }

        [RequireServerFact]
        public void ShouldHandleCreateMultipleNodesWithProperties()
        {
            using (var session = Driver.Session())
            {
                var result = session.Run("UNWIND $props AS properties CREATE(n:Person) SET n = properties",
                    new {props = new object[]
                    {
                        new { awesome = true, name = "Andres", position = "Developer"},
                        new { children = 3, name = "Michael", position = "Developer"}
                    }
                    });
                result.Summary.Counters.NodesCreated.Should().Be(2);

                result = session.Run("MATCH (n) WHERE n.position = 'Developer' RETURN n.name");
                var list = result.Select(r => r[0]).ToList();
                list.Should().HaveCount(2);
                list.Should().Contain("Andres", "Michael");
            }
        }

        [RequireServerFact]
        public void ShouldHandleSettingAllPropertiesOnANode()
        {
            using (var session = Driver.Session())
            {
                var result = session.Run("CREATE (n:Person { name: 'Michaela' })");
                result.Summary.Counters.NodesCreated.Should().Be(1);

                result = session.Run("MATCH (n:Person) WHERE n.name = 'Michaela' SET n = $props",
                    new {props = new {name = "Andres", position = "Developer"}});
                result.Summary.Counters.PropertiesSet.Should().Be(2);

                result = session.Run("MATCH (n:Person) WHERE n.name = $name RETURN n.position", new { name = "Andres" });
                var list = result.Select(r => r[0]).ToList();
                list.Should().HaveCount(1);
                list.Should().Contain("Developer");
            }
        }

        [RequireServerFact]
        public void ShouldHandleSkipAndLimit()
        {
            using (var session = Driver.Session())
            {
                var result = session.Run("UNWIND range(1,1000) as number RETURN number SKIP $skip LIMIT $limit",
                    new {skip = 100, limit = 1});

                var list = result.Select(r => (long)r[0]).ToList();
                list.Should().HaveCount(1);
                list.Should().Contain(101L);
            }
        }

        [RequireServerFact]
        public void ShouldHandleNodeId()
        {
            using (var session = Driver.Session())
            {
                var result = session.Run("CREATE (n:Person { name: 'Michaela' }) RETURN id(n)");
                var id = result.Select(r => (long)r[0]).Single();
                result.Summary.Counters.NodesCreated.Should().Be(1);

                result = session.Run("MATCH (n) WHERE id(n) = $id RETURN n.name", new {id = id});
                var list = result.Select(r => r[0]).ToList();
                list.Should().HaveCount(1);
                list.Should().Contain("Michaela");
            }
        }

        [RequireServerFact]
        public void ShouldHandleMultipleNodeIds()
        {
            using (var session = Driver.Session())
            {
                var result = session.Run("UNWIND $props AS properties CREATE(n:Person) SET n = properties RETURN id(n)",
                    new
                    {
                        props = new List<object>()
                        {
                            new {name = "Johan"},
                            new {name = "Michaela"},
                            new {name = "Andres"}
                        }
                    });
                var ids = result.Select(r => (long)r[0]).ToArray();
                result.Summary.Counters.NodesCreated.Should().Be(3);

                result = session.Run("MATCH (n) WHERE id(n) IN $idList RETURN n.name", new { idList = ids });
                var list = result.Select(r => r[0]).ToList();
                list.Should().HaveCount(3);
                list.Should().Contain("Johan", "Michaela", "Andres");
            }
        }

        [RequireServerVersionLessThanFact("4.0.0")] // jmx procedure is not accessible
        public void ShouldHandleCallingProcedures()
        {
            using (var session = Driver.Session())
            {
                var result = session.Run("CALL dbms.queryJmx($query) yield name", new { query = "org.neo4j:*" });
                var names = result.Select(r => r[0]);

                names.Should().HaveCount(c => c > 0);
            }
        }

    }
}
