// Copyright (c) 2002-2018 Neo4j Sweden AB [http://neo4j.com]
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
using System.Linq;
using FluentAssertions;
using Neo4j.Driver.V1;
using Xunit;

namespace Neo4j.Driver.Tck.Tests.TCK
{
    public class CypherRecordParserTests
    {
        private static readonly CypherRecordParser Parser = new CypherRecordParser();

        public class ParseBaseMethod
        {
            [Theory]
            [InlineData("True", true)]
            [InlineData("true", true)]
            [InlineData("False", false)]
            [InlineData("false", false)]
            [InlineData("faLSE", false)]
            public void ShouldParseToBool(string value, bool expected)
            {
                Assert.True(Parser.Parse(value) == expected);
            }

            [Theory]
            [InlineData("Null")]
            [InlineData("NULL")]
            [InlineData("null")]
            [InlineData("nuLL")]
            public void ShouldParseToNull(string value)
            {
                Assert.Null(Parser.Parse(value));
            }

            [Theory]
            [InlineData("\"Null\"", "Null")]
            [InlineData("\"\"", "")]
            [InlineData("\"lala lalal\"", "lala lalal")]
            public void ShouldParseToString(string value, string expected)
            {
                string str = Parser.Parse(value);
                str.Should().Be(expected);
            }

            [Theory]
            [InlineData("1.1", 1.1)]
            [InlineData("+1.1", 1.1)]
            [InlineData("-1.1", -1.1)]
            [InlineData("1.04E-1", 1.04E-1)]
            [InlineData("-1.04E-1", -1.04E-1)]
            [InlineData("-1.04E+1", -1.04E+1)]
            public void ShouldParseToFloat(string value, double expected)
            {
                double actual = Parser.Parse(value);
                actual.Should().Be(expected);
            }

            [Theory]
            [InlineData("0", 0)]
            [InlineData("+1", 1)]
            [InlineData("-1", -1)]
            public void ShouldParseToInt64(string value, long expected)
            {
                long actual = Parser.Parse(value);
                actual.Should().Be(expected);
            }
        }

        public class ParseMapMethod
        {
            [Theory]
            [InlineData("{ }")]
            [InlineData("{}")]
            public void ShouldParseEmptyMap(string input)
            {
                var actual = Parser.ParseMap(input);
                actual.Count.Should().Be(0);
            }

            [Theory]
            [InlineData("{\"value\": 2}")]
            [InlineData("{ \"value\" : 2 }")]
            public void ShouldParseToMap(string value)
            {
                var actual = Parser.ParseMap(value);
                actual.Count.Should().Be(1);
                actual.ContainsKey("value").Should().BeTrue();
                ((long)actual["value"]).Should().Be(2L);
            }

            [Theory]
            [InlineData("{\"value\": 2 \"key\": \"key\"}")]
            [InlineData("{ \"value\" : 2 \"key\" : \"key\" }")]
            public void ShouldParseMultiItemsToMap(string input)
            {
                var actual = Parser.ParseMap(input);
                actual.Count.Should().Be(2);
                actual.ContainsKey("value").Should().BeTrue();
                actual.ContainsKey("key").Should().BeTrue();
                ((long)actual["value"]).Should().Be(2L);
                ((string)actual["key"]).Should().Be("key");
            }
        }

        public class ParseNode
        {
            [Theory]
            [InlineData("(:label1:label2)")]
            [InlineData("( : label1 : label2 )")]
            public void ShouldParseNodeWithOnlyLabels(string input)
            {
                var actual = Parser.ParseNode(input);
                actual.Labels.Count.Should().Be(2);
                actual.Labels.Contains("label1").Should().BeTrue();
                actual.Labels.Contains("label2").Should().BeTrue();
                actual.Properties.Count.Should().Be(0);
            }

            [Theory]
            [InlineData("({\"key\":\"bbb\"})")]
            [InlineData("( { \"key\" : \"bbb\" } )")]
            public void ShouldParseNodeWithOnlyProps(string input)
            {
                var actual = Parser.ParseNode(input);
                actual.Labels.Count.Should().Be(0);
                actual.Properties.Count.Should().Be(1);
                actual.Properties.Keys.Contains("key").Should().BeTrue();
                ((string) actual.Properties["key"]).Should().Be("bbb");
            }

            [Theory]
            [InlineData("(:label1:label2 {\"value\":12})")]
            [InlineData("( : label1 : label2 { \"value\" : 12 } )")]
            public void ShouldParseNode(string input)
            {
                var actual = Parser.ParseNode(input);
                actual.Labels.Count.Should().Be(2);
                actual.Labels.Contains("label1").Should().BeTrue();
                actual.Labels.Contains("label2").Should().BeTrue();
                actual.Properties.Count.Should().Be(1);
                actual.Properties.Keys.Contains("value").Should().BeTrue();
                ((long)actual.Properties["value"]).Should().Be(12L);
            }
        }

        public class ParseRelationshipMethod
        {
            [Theory]
            [InlineData("[:type]")]
            [InlineData("[ : type ]")]
            public void ShouldParseRelWithOnlyType(string input)
            {
                var actual = Parser.ParseRelationship(input);
                actual.Type.Should().Be("type");
                actual.Properties.Count.Should().Be(0);
            }

            [Theory]
            [InlineData("[:type1 {\"value\":12}]")]
            [InlineData("[ : type1 { \"value\" : 12 } ]")]
            public void ShouldParseRel(string input)
            {
                var actual = Parser.ParseRelationship(input);
                actual.Type.Should().Be("type1");
                actual.Properties.Count.Should().Be(1);
                actual.Properties.Keys.Contains("value").Should().BeTrue();
                ((long)actual.Properties["value"]).Should().Be(12L);
            }
        }

        public class ParsePathMethod
        {
            [Fact]
            public void ShouldParseZeroLengthPath()
            {
                var actual = Parser.ParsePath("<(:A {\"name\": \"A\"})>");
                var labels = actual.Start.Labels;
                labels.Count.Should().Be(1);
                labels.Contains("A").Should().BeTrue();
                var props = actual.Start.Properties;
                props.Count.Should().Be(1);
                props.Keys.Contains("name").Should().BeTrue();
                ((string)props["name"]).Should().Be("A");
                CypherRecordParser.PathToString(actual).Should().Be("<([A] [{name, A}])>");
            }

            [Fact]
            public void ShouldParsePath()
            {
                var actual = Parser.ParsePath("<(:A {\"name\": \"A\"})-[:KNOWS {\"value\": 1}]->(:B {\"name\": \"B\"})<-[:KNOWS {\"value\": 2}]-(:C {\"name\": \"C\"})>");
                actual.Nodes.Count.Should().Be(3);
                actual.Relationships.Count.Should().Be(2);

                var labels = actual.Start.Labels;
                labels.Count.Should().Be(1);
                labels.Contains("A").Should().BeTrue();
                var props = actual.Start.Properties;
                props.Count.Should().Be(1);
                props.Keys.Contains("name").Should().BeTrue();
                ((string)props["name"]).Should().Be("A");


                labels = actual.End.Labels;
                labels.Count.Should().Be(1);
                labels.Contains("C").Should().BeTrue();
                props = actual.End.Properties;
                props.Count.Should().Be(1);
                props.Keys.Contains("name").Should().BeTrue();
                ((string)props["name"]).Should().Be("C");

                CypherRecordParser.PathToString(actual).Should().Be("<([A] [{name, A}])-[KNOWS [{value, 1}]]->([B] [{name, B}])<-[KNOWS [{value, 2}]]-([C] [{name, C}])>");
            }
        }

        public class ParseListMethod
        {
            [Theory]
            [InlineData("[]")]
            [InlineData("[ ]")]
            public void ShouldParseEmptyList(string input)
            {
                var actual = Parser.ParseList(input);
                actual.Count.Should().Be(0);
            }

            [Theory]
            [InlineData("[[:REL1], [:REL2]]")]
            [InlineData("[ [ : REL1 ] , [ : REL2 ] ]")]
            public void ShouldParseRelList(string input)
            {
                var actual = Parser.ParseList(input);
                actual.Count.Should().Be(2);

                IRelationship rel = (IRelationship)actual[0];
                rel.Type.Should().Be("REL1");
                rel.Properties.Count.Should().Be(0);

                rel = (IRelationship)actual[1];
                rel.Type.Should().Be("REL2");
                rel.Properties.Count.Should().Be(0);
            }
        }
    }
}
