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
#region Designer generated code
#pragma warning disable
namespace Neo4j.Driver.Tck.Tests.TCK
{
    using TechTalk.SpecFlow;
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("TechTalk.SpecFlow", "2.0.0.0")]
    [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [Xunit.TraitAttribute("Category", "match_acceptance")]
    [Xunit.TraitAttribute("Category", "reset_database")]
    public partial class MatchAcceptanceTestFeature : Xunit.IClassFixture<MatchAcceptanceTestFeature.FixtureData>, System.IDisposable
    {
        
        private static TechTalk.SpecFlow.ITestRunner testRunner;
        
#line 1 "MatchAcceptanceTest.feature"
#line hidden
        
        public MatchAcceptanceTestFeature()
        {
            this.TestInitialize();
        }
        
        public static void FeatureSetup()
        {
            testRunner = TechTalk.SpecFlow.TestRunnerManager.GetTestRunner();
            TechTalk.SpecFlow.FeatureInfo featureInfo = new TechTalk.SpecFlow.FeatureInfo(new System.Globalization.CultureInfo("en-US"), "MatchAcceptanceTest", null, ProgrammingLanguage.CSharp, new string[] {
                        "match_acceptance",
                        "reset_database"});
            testRunner.OnFeatureStart(featureInfo);
        }
        
        public static void FeatureTearDown()
        {
            testRunner.OnFeatureEnd();
            testRunner = null;
        }
        
        public virtual void TestInitialize()
        {
        }
        
        public virtual void ScenarioTearDown()
        {
            testRunner.OnScenarioEnd();
        }
        
        public virtual void ScenarioSetup(TechTalk.SpecFlow.ScenarioInfo scenarioInfo)
        {
            testRunner.OnScenarioStart(scenarioInfo);
        }
        
        public virtual void ScenarioCleanup()
        {
            testRunner.CollectScenarioErrors();
        }
        
        public virtual void SetFixture(MatchAcceptanceTestFeature.FixtureData fixtureData)
        {
        }
        
        void System.IDisposable.Dispose()
        {
            this.ScenarioTearDown();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "MatchAcceptanceTest")]
        [Xunit.TraitAttribute("Description", "path query should return results in written order")]
        public virtual void PathQueryShouldReturnResultsInWrittenOrder()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("path query should return results in written order", ((string[])(null)));
#line 4
  this.ScenarioSetup(scenarioInfo);
#line 5
    testRunner.Given("init: CREATE (:label1)<-[:TYPE]-(:label2);", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 6
    testRunner.When("running: MATCH (a:label1) RETURN (a)<--(:label2) AS p;", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table1 = new TechTalk.SpecFlow.Table(new string[] {
                        "p"});
            table1.AddRow(new string[] {
                        "[<(:label1)<-[:TYPE]-(:label2)>]"});
#line 7
    testRunner.Then("result:", ((string)(null)), table1, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "MatchAcceptanceTest")]
        [Xunit.TraitAttribute("Description", "longer path query should return results in written order")]
        public virtual void LongerPathQueryShouldReturnResultsInWrittenOrder()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("longer path query should return results in written order", ((string[])(null)));
#line 11
  this.ScenarioSetup(scenarioInfo);
#line 12
    testRunner.Given("init: CREATE (:label1)<-[:T1]-(:label2)-[:T2]->(:label3);", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 13
    testRunner.When("running: MATCH (a:label1) RETURN (a)<--(:label2)--() AS p;", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table2 = new TechTalk.SpecFlow.Table(new string[] {
                        "p"});
            table2.AddRow(new string[] {
                        "[<(:label1)<-[:T1]-(:label2)-[:T2]->(:label3)>]"});
#line 14
    testRunner.Then("result:", ((string)(null)), table2, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "MatchAcceptanceTest")]
        [Xunit.TraitAttribute("Description", "Get node degree via length of pattern expression")]
        public virtual void GetNodeDegreeViaLengthOfPatternExpression()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Get node degree via length of pattern expression", ((string[])(null)));
#line 18
  this.ScenarioSetup(scenarioInfo);
#line 19
    testRunner.Given("init: CREATE (x:X), (x)-[:T]->(), (x)-[:T]->(), (x)-[:T]->();", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 20
    testRunner.When("running: MATCH (a:X) RETURN length((a)-->()) as length;", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table3 = new TechTalk.SpecFlow.Table(new string[] {
                        "length"});
            table3.AddRow(new string[] {
                        "3"});
#line 21
    testRunner.Then("result:", ((string)(null)), table3, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "MatchAcceptanceTest")]
        [Xunit.TraitAttribute("Description", "Get node degree via length of pattern expression that specifies a relationship ty" +
            "pe")]
        public virtual void GetNodeDegreeViaLengthOfPatternExpressionThatSpecifiesARelationshipType()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Get node degree via length of pattern expression that specifies a relationship ty" +
                    "pe", ((string[])(null)));
#line 25
  this.ScenarioSetup(scenarioInfo);
#line 26
    testRunner.Given("init: CREATE (x:X), (x)-[:T]->(), (x)-[:T]->(), (x)-[:T]->(), (x)-[:AFFE]->();", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 27
    testRunner.When("running: MATCH (a:X) RETURN length((a)-[:T]->()) as length;", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table4 = new TechTalk.SpecFlow.Table(new string[] {
                        "length"});
            table4.AddRow(new string[] {
                        "3"});
#line 28
    testRunner.Then("result:", ((string)(null)), table4, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "MatchAcceptanceTest")]
        [Xunit.TraitAttribute("Description", "Get node degree via length of pattern expression that specifies multiple relation" +
            "ship types")]
        public virtual void GetNodeDegreeViaLengthOfPatternExpressionThatSpecifiesMultipleRelationshipTypes()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Get node degree via length of pattern expression that specifies multiple relation" +
                    "ship types", ((string[])(null)));
#line 32
  this.ScenarioSetup(scenarioInfo);
#line 33
    testRunner.Given("init: CREATE (x:X), (x)-[:T]->(), (x)-[:T]->(), (x)-[:T]->(), (x)-[:AFFE]->();", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 34
    testRunner.When("running: MATCH (a:X) RETURN length((a)-[:T|AFFE]->()) as length;", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table5 = new TechTalk.SpecFlow.Table(new string[] {
                        "length"});
            table5.AddRow(new string[] {
                        "4"});
#line 35
    testRunner.Then("result:", ((string)(null)), table5, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "MatchAcceptanceTest")]
        [Xunit.TraitAttribute("Description", "should be able to use multiple MATCH clauses to do a cartesian product")]
        public virtual void ShouldBeAbleToUseMultipleMATCHClausesToDoACartesianProduct()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("should be able to use multiple MATCH clauses to do a cartesian product", ((string[])(null)));
#line 39
  this.ScenarioSetup(scenarioInfo);
#line 40
    testRunner.Given("init: CREATE ({value: 1}), ({value: 2}), ({value: 3});", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 41
    testRunner.When("running: MATCH (n), (m) RETURN n.value AS n, m.value AS m;", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table6 = new TechTalk.SpecFlow.Table(new string[] {
                        "n",
                        "m"});
            table6.AddRow(new string[] {
                        "1",
                        "1"});
            table6.AddRow(new string[] {
                        "1",
                        "2"});
            table6.AddRow(new string[] {
                        "1",
                        "3"});
            table6.AddRow(new string[] {
                        "2",
                        "1"});
            table6.AddRow(new string[] {
                        "2",
                        "2"});
            table6.AddRow(new string[] {
                        "2",
                        "3"});
            table6.AddRow(new string[] {
                        "3",
                        "3"});
            table6.AddRow(new string[] {
                        "3",
                        "1"});
            table6.AddRow(new string[] {
                        "3",
                        "2"});
#line 42
    testRunner.Then("result:", ((string)(null)), table6, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "MatchAcceptanceTest")]
        [Xunit.TraitAttribute("Description", "should be able to use params in pattern matching predicates")]
        public virtual void ShouldBeAbleToUseParamsInPatternMatchingPredicates()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("should be able to use params in pattern matching predicates", ((string[])(null)));
#line 54
  this.ScenarioSetup(scenarioInfo);
#line 55
    testRunner.Given("init: CREATE (:a)-[:A {foo: \"bar\"}]->(:b {name: \'me\'})", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line hidden
            TechTalk.SpecFlow.Table table7 = new TechTalk.SpecFlow.Table(new string[] {
                        "param"});
            table7.AddRow(new string[] {
                        "\"bar\""});
#line 56
    testRunner.When("running parametrized: match (a)-[r]->(b) where r.foo =~ {param} return b", ((string)(null)), table7, "When ");
#line hidden
            TechTalk.SpecFlow.Table table8 = new TechTalk.SpecFlow.Table(new string[] {
                        "b"});
            table8.AddRow(new string[] {
                        "(:b {\"name\": \"me\"})"});
#line 59
    testRunner.Then("result:", ((string)(null)), table8, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute(Skip="Ignored")]
        [Xunit.TraitAttribute("FeatureTitle", "MatchAcceptanceTest")]
        [Xunit.TraitAttribute("Description", "should make query from existing database")]
        public virtual void ShouldMakeQueryFromExistingDatabase()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("should make query from existing database", new string[] {
                        "ignore"});
#line 64
  this.ScenarioSetup(scenarioInfo);
#line 65
    testRunner.Given("using: cineast", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 66
    testRunner.When("running: MATCH (n) RETURN count(n)", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table9 = new TechTalk.SpecFlow.Table(new string[] {
                        "count(n)"});
            table9.AddRow(new string[] {
                        "63084"});
#line 67
    testRunner.Then("result:", ((string)(null)), table9, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "MatchAcceptanceTest")]
        [Xunit.TraitAttribute("Description", "should filter out based on node prop name")]
        public virtual void ShouldFilterOutBasedOnNodePropName()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("should filter out based on node prop name", ((string[])(null)));
#line 71
  this.ScenarioSetup(scenarioInfo);
#line 72
    testRunner.Given("init: CREATE ({name: \"Someone Else\"})<-[:x]-()-[:x]->({name: \"Andres\"})", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 73
    testRunner.When("running: MATCH (start)-[rel:x]-(a) WHERE a.name = \'Andres\' return a", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table10 = new TechTalk.SpecFlow.Table(new string[] {
                        "a"});
            table10.AddRow(new string[] {
                        "({\"name\": \"Andres\"})"});
#line 74
    testRunner.Then("result:", ((string)(null)), table10, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "MatchAcceptanceTest")]
        [Xunit.TraitAttribute("Description", "should honour the column name for RETURN items")]
        public virtual void ShouldHonourTheColumnNameForRETURNItems()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("should honour the column name for RETURN items", ((string[])(null)));
#line 78
  this.ScenarioSetup(scenarioInfo);
#line 79
    testRunner.Given("init: CREATE ({name: \"Someone Else\"})", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 80
    testRunner.When("running: MATCH (a) WITH a.name AS a RETURN a", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table11 = new TechTalk.SpecFlow.Table(new string[] {
                        "a"});
            table11.AddRow(new string[] {
                        "\"Someone Else\""});
#line 81
    testRunner.Then("result:", ((string)(null)), table11, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "MatchAcceptanceTest")]
        [Xunit.TraitAttribute("Description", "should filter based on rel prop name")]
        public virtual void ShouldFilterBasedOnRelPropName()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("should filter based on rel prop name", ((string[])(null)));
#line 85
  this.ScenarioSetup(scenarioInfo);
#line 86
    testRunner.Given("init: CREATE (:a)<-[:KNOWS {name: \"monkey\"}]-()-[:KNOWS {name: \"woot\"}]->(:b)", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 87
    testRunner.When("running: match (node)-[r:KNOWS]->(a) WHERE r.name = \'monkey\' RETURN a", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table12 = new TechTalk.SpecFlow.Table(new string[] {
                        "a"});
            table12.AddRow(new string[] {
                        "(:a)"});
#line 88
    testRunner.Then("result:", ((string)(null)), table12, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "MatchAcceptanceTest")]
        [Xunit.TraitAttribute("Description", "should cope with shadowed variables")]
        public virtual void ShouldCopeWithShadowedVariables()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("should cope with shadowed variables", ((string[])(null)));
#line 92
  this.ScenarioSetup(scenarioInfo);
#line 93
    testRunner.Given("init: CREATE ({value: 1, name: \'King Kong\'}), ({value: 2, name: \'Ann Darrow\'})", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 94
    testRunner.When("running: MATCH (n) WITH n.name AS n RETURN n", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table13 = new TechTalk.SpecFlow.Table(new string[] {
                        "n"});
            table13.AddRow(new string[] {
                        "\"Ann Darrow\""});
            table13.AddRow(new string[] {
                        "\"King Kong\""});
#line 95
    testRunner.Then("result:", ((string)(null)), table13, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "MatchAcceptanceTest")]
        [Xunit.TraitAttribute("Description", "should get neighbours")]
        public virtual void ShouldGetNeighbours()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("should get neighbours", ((string[])(null)));
#line 100
  this.ScenarioSetup(scenarioInfo);
#line 101
    testRunner.Given("init: CREATE (a:A {value : 1})-[:KNOWS]->(b:B {value : 2})", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 102
    testRunner.When("running: MATCH (n1)-[rel:KNOWS]->(n2) RETURN n1, n2", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table14 = new TechTalk.SpecFlow.Table(new string[] {
                        "n1",
                        "n2"});
            table14.AddRow(new string[] {
                        "(:A {\"value\": 1})",
                        "(:B {\"value\": 2})"});
#line 103
    testRunner.Then("result:", ((string)(null)), table14, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "MatchAcceptanceTest")]
        [Xunit.TraitAttribute("Description", "should get two related nodes")]
        public virtual void ShouldGetTwoRelatedNodes()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("should get two related nodes", ((string[])(null)));
#line 107
  this.ScenarioSetup(scenarioInfo);
#line 108
    testRunner.Given("init: CREATE (a:A {value: 1}), (a)-[:KNOWS]->(b:B {value: 2}), (a)-[:KNOWS]->(c:C" +
                    " {value: 3})", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 109
    testRunner.When("running: MATCH (start)-[rel:KNOWS]->(x) RETURN x", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table15 = new TechTalk.SpecFlow.Table(new string[] {
                        "x"});
            table15.AddRow(new string[] {
                        "(:B {\"value\": 2})"});
            table15.AddRow(new string[] {
                        "(:C {\"value\": 3})"});
#line 110
    testRunner.Then("result:", ((string)(null)), table15, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "MatchAcceptanceTest")]
        [Xunit.TraitAttribute("Description", "should get related to related to")]
        public virtual void ShouldGetRelatedToRelatedTo()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("should get related to related to", ((string[])(null)));
#line 115
  this.ScenarioSetup(scenarioInfo);
#line 116
    testRunner.Given("init: CREATE (a:A {value: 1})-[:KNOWS]->(b:B {value: 2})-[:FRIEND]->(c:C {value: " +
                    "3})", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 117
    testRunner.When("running: MATCH (n)-->(a)-->(b) RETURN b", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table16 = new TechTalk.SpecFlow.Table(new string[] {
                        "b"});
            table16.AddRow(new string[] {
                        "(:C {\"value\": 3})"});
#line 118
    testRunner.Then("result:", ((string)(null)), table16, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "MatchAcceptanceTest")]
        [Xunit.TraitAttribute("Description", "should handle comparison between node properties")]
        public virtual void ShouldHandleComparisonBetweenNodeProperties()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("should handle comparison between node properties", ((string[])(null)));
#line 122
  this.ScenarioSetup(scenarioInfo);
#line 123
    testRunner.Given("init: CREATE (a:A {animal: \"monkey\"}), (b:B {animal: \"cow\"}), (c:C {animal: \"monk" +
                    "ey\"}), (d:D {animal: \"cow\"}), (a)-[:KNOWS]->(b), (a)-[:KNOWS]->(c), (d)-[:KNOWS]" +
                    "->(b), (d)-[:KNOWS]->(c)", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 124
    testRunner.When("running: MATCH (n)-[rel]->(x) WHERE n.animal = x.animal RETURN n, x", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table17 = new TechTalk.SpecFlow.Table(new string[] {
                        "n",
                        "x"});
            table17.AddRow(new string[] {
                        "(:A {\"animal\": \"monkey\"})",
                        "(:C {\"animal\": \"monkey\"})"});
            table17.AddRow(new string[] {
                        "(:D {\"animal\": \"cow\"})",
                        "(:B {\"animal\": \"cow\"})"});
#line 125
    testRunner.Then("result:", ((string)(null)), table17, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "MatchAcceptanceTest")]
        [Xunit.TraitAttribute("Description", "should return two subgraphs with bound undirected relationship")]
        public virtual void ShouldReturnTwoSubgraphsWithBoundUndirectedRelationship()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("should return two subgraphs with bound undirected relationship", ((string[])(null)));
#line 130
  this.ScenarioSetup(scenarioInfo);
#line 131
    testRunner.Given("init: CREATE (a:A {value: 1})-[:REL {name: \"r\"}]->(b:B {value: 2})", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 132
    testRunner.When("running: match (a)-[r {name: \'r\'}]-(b) RETURN a,b", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table18 = new TechTalk.SpecFlow.Table(new string[] {
                        "a",
                        "b"});
            table18.AddRow(new string[] {
                        "(:B {\"value\": 2})",
                        "(:A {\"value\": 1})"});
            table18.AddRow(new string[] {
                        "(:A {\"value\": 1})",
                        "(:B {\"value\": 2})"});
#line 133
    testRunner.Then("result:", ((string)(null)), table18, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "MatchAcceptanceTest")]
        [Xunit.TraitAttribute("Description", "should return two subgraphs with bound undirected relationship and optional relat" +
            "ionship")]
        public virtual void ShouldReturnTwoSubgraphsWithBoundUndirectedRelationshipAndOptionalRelationship()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("should return two subgraphs with bound undirected relationship and optional relat" +
                    "ionship", ((string[])(null)));
#line 138
  this.ScenarioSetup(scenarioInfo);
#line 139
    testRunner.Given("init: CREATE (a:A {value: 1})-[:REL {name: \"r1\"}]->(b:B {value: 2})-[:REL {name: " +
                    "\"r2\"}]->(c:C {value: 3})", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 140
    testRunner.When("running: MATCH (a)-[r {name:\'r1\'}]-(b) OPTIONAL MATCH (b)-[r2]-(c) WHERE r<>r2 RE" +
                    "TURN a,b,c", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table19 = new TechTalk.SpecFlow.Table(new string[] {
                        "a",
                        "b",
                        "c"});
            table19.AddRow(new string[] {
                        "(:A {\"value\": 1})",
                        "(:B {\"value\": 2})",
                        "(:C {\"value\": 3})"});
            table19.AddRow(new string[] {
                        "(:B {\"value\": 2})",
                        "(:A {\"value\": 1})",
                        "null"});
#line 141
    testRunner.Then("result:", ((string)(null)), table19, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "MatchAcceptanceTest")]
        [Xunit.TraitAttribute("Description", "rel type function works as expected")]
        public virtual void RelTypeFunctionWorksAsExpected()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("rel type function works as expected", ((string[])(null)));
#line 146
  this.ScenarioSetup(scenarioInfo);
#line 147
    testRunner.Given("init: CREATE (a:A {name: \"A\"}), (b:B {name: \"B\"}), (c:C {name: \"C\"}), (a)-[:KNOWS" +
                    "]->(b), (a)-[:HATES]->(c)", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 148
    testRunner.When("running: MATCH (n {name:\'A\'})-[r]->(x) WHERE type(r) = \'KNOWS\' RETURN x", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table20 = new TechTalk.SpecFlow.Table(new string[] {
                        "x"});
            table20.AddRow(new string[] {
                        "(:B {\"name\": \"B\"})"});
#line 149
    testRunner.Then("result:", ((string)(null)), table20, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "MatchAcceptanceTest")]
        [Xunit.TraitAttribute("Description", "should walk alternative relationships")]
        public virtual void ShouldWalkAlternativeRelationships()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("should walk alternative relationships", ((string[])(null)));
#line 153
  this.ScenarioSetup(scenarioInfo);
#line 154
    testRunner.Given("init: CREATE (a {name: \"A\"}), (b {name: \"B\"}), (c {name: \"C\"}), (a)-[:KNOWS]->(b)" +
                    ", (a)-[:HATES]->(c), (a)-[:WONDERS]->(c)", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 155
    testRunner.When("running: MATCH (n)-[r]->(x) WHERE type(r) = \'KNOWS\' OR type(r) = \'HATES\' RETURN r" +
                    "", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table21 = new TechTalk.SpecFlow.Table(new string[] {
                        "r"});
            table21.AddRow(new string[] {
                        "[:KNOWS]"});
            table21.AddRow(new string[] {
                        "[:HATES]"});
#line 156
    testRunner.Then("result:", ((string)(null)), table21, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "MatchAcceptanceTest")]
        [Xunit.TraitAttribute("Description", "should handle OR in the WHERE clause")]
        public virtual void ShouldHandleORInTheWHEREClause()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("should handle OR in the WHERE clause", ((string[])(null)));
#line 161
  this.ScenarioSetup(scenarioInfo);
#line 162
    testRunner.Given("init: CREATE (a:A {p1: 12}), (b:B {p2: 13}), (c:C)", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 163
    testRunner.When("running: MATCH (n) WHERE n.p1 = 12 OR n.p2 = 13 RETURN n", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table22 = new TechTalk.SpecFlow.Table(new string[] {
                        "n"});
            table22.AddRow(new string[] {
                        "(:A {\"p1\": 12})"});
            table22.AddRow(new string[] {
                        "(:B {\"p2\": 13})"});
#line 164
    testRunner.Then("result:", ((string)(null)), table22, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "MatchAcceptanceTest")]
        [Xunit.TraitAttribute("Description", "should return a simple path")]
        public virtual void ShouldReturnASimplePath()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("should return a simple path", ((string[])(null)));
#line 169
  this.ScenarioSetup(scenarioInfo);
#line 170
    testRunner.Given("init: CREATE (a:A {name: \"A\"})-[:KNOWS]->(b:B {name: \"B\"})", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 171
    testRunner.When("running: MATCH p=(a {name:\'A\'})-->(b) RETURN p", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table23 = new TechTalk.SpecFlow.Table(new string[] {
                        "p"});
            table23.AddRow(new string[] {
                        "<(:A {\"name\": \"A\"})-[:KNOWS]->(:B {\"name\": \"B\"})>"});
#line 172
    testRunner.Then("result:", ((string)(null)), table23, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "MatchAcceptanceTest")]
        [Xunit.TraitAttribute("Description", "should return a three node path")]
        public virtual void ShouldReturnAThreeNodePath()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("should return a three node path", ((string[])(null)));
#line 176
  this.ScenarioSetup(scenarioInfo);
#line 177
    testRunner.Given("init: CREATE (a:A {name: \"A\"})-[:KNOWS]->(b:B {name: \"B\"})-[:KNOWS]->(c:C {name: " +
                    "\"C\"})", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 178
    testRunner.When("running: MATCH p = (a {name:\'A\'})-[rel1]->(b)-[rel2]->(c) RETURN p", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table24 = new TechTalk.SpecFlow.Table(new string[] {
                        "p"});
            table24.AddRow(new string[] {
                        "<(:A {\"name\": \"A\"})-[:KNOWS]->(:B {\"name\": \"B\"})-[:KNOWS]->(:C {\"name\": \"C\"})>"});
#line 179
    testRunner.Then("result:", ((string)(null)), table24, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "MatchAcceptanceTest")]
        [Xunit.TraitAttribute("Description", "should not return anything because path length does not match")]
        public virtual void ShouldNotReturnAnythingBecausePathLengthDoesNotMatch()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("should not return anything because path length does not match", ((string[])(null)));
#line 183
  this.ScenarioSetup(scenarioInfo);
#line 184
    testRunner.Given("init: CREATE (a:A {name: \"A\"})-[:KNOWS]->(b:B {name: \"B\"})", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 185
    testRunner.When("running: MATCH p = (n)-->(x) WHERE length(p) = 10 RETURN x", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table25 = new TechTalk.SpecFlow.Table(new string[] {
                        ""});
#line 186
    testRunner.Then("result:", ((string)(null)), table25, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "MatchAcceptanceTest")]
        [Xunit.TraitAttribute("Description", "should pass the path length test")]
        public virtual void ShouldPassThePathLengthTest()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("should pass the path length test", ((string[])(null)));
#line 189
  this.ScenarioSetup(scenarioInfo);
#line 190
    testRunner.Given("init: CREATE (a:A {name: \"A\"})-[:KNOWS]->(b:B {name: \"B\"})", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 191
    testRunner.When("running: MATCH p = (n)-->(x) WHERE length(p)=1 RETURN x", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table26 = new TechTalk.SpecFlow.Table(new string[] {
                        "x"});
            table26.AddRow(new string[] {
                        "(:B {\"name\": \"B\"})"});
#line 192
    testRunner.Then("result:", ((string)(null)), table26, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "MatchAcceptanceTest")]
        [Xunit.TraitAttribute("Description", "should be able to filter on path nodes")]
        public virtual void ShouldBeAbleToFilterOnPathNodes()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("should be able to filter on path nodes", ((string[])(null)));
#line 196
  this.ScenarioSetup(scenarioInfo);
#line 197
    testRunner.Given("init: CREATE (a:A {foo: \"bar\"})-[:REL]->(b:B {foo: \"bar\"})-[:REL]->(c:C {foo: \"ba" +
                    "r\"})-[:REL]->(d:D {foo: \"bar\"})", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 198
    testRunner.When("running: MATCH p = (pA)-[:REL*3..3]->(pB) WHERE all(i in nodes(p) WHERE i.foo = \'" +
                    "bar\') RETURN pB", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table27 = new TechTalk.SpecFlow.Table(new string[] {
                        "pB"});
            table27.AddRow(new string[] {
                        "(:D {\"foo\": \"bar\"})"});
#line 199
    testRunner.Then("result:", ((string)(null)), table27, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "MatchAcceptanceTest")]
        [Xunit.TraitAttribute("Description", "should return relationships by fetching them from the path - starting from the en" +
            "d")]
        public virtual void ShouldReturnRelationshipsByFetchingThemFromThePath_StartingFromTheEnd()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("should return relationships by fetching them from the path - starting from the en" +
                    "d", ((string[])(null)));
#line 203
  this.ScenarioSetup(scenarioInfo);
#line 204
    testRunner.Given("init: CREATE (a:A)-[:REL {value: 1}]->(b:B)-[:REL {value: 2}]->(e:End)", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 205
    testRunner.When("running: MATCH p = (a)-[:REL*2..2]->(b:End) RETURN relationships(p)", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table28 = new TechTalk.SpecFlow.Table(new string[] {
                        "relationships(p)"});
            table28.AddRow(new string[] {
                        "[[:REL {\"value\": 1}], [:REL {\"value\": 2}]]"});
#line 206
    testRunner.Then("result:", ((string)(null)), table28, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "MatchAcceptanceTest")]
        [Xunit.TraitAttribute("Description", "should return relationships by fetching them from the path")]
        public virtual void ShouldReturnRelationshipsByFetchingThemFromThePath()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("should return relationships by fetching them from the path", ((string[])(null)));
#line 210
  this.ScenarioSetup(scenarioInfo);
#line 211
    testRunner.Given("init: CREATE (s:Start)-[:REL {value: 1}]->(b:B)-[:REL {value: 2}]->(c:C)", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 212
    testRunner.When("running: MATCH p = (a:Start)-[:REL*2..2]->(b) RETURN relationships(p)", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table29 = new TechTalk.SpecFlow.Table(new string[] {
                        "relationships(p)"});
            table29.AddRow(new string[] {
                        "[[:REL {\"value\": 1}], [:REL {\"value\": 2}]]"});
#line 213
    testRunner.Then("result:", ((string)(null)), table29, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "MatchAcceptanceTest")]
        [Xunit.TraitAttribute("Description", "should return relationships by collecting them as a list - wrong way")]
        public virtual void ShouldReturnRelationshipsByCollectingThemAsAList_WrongWay()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("should return relationships by collecting them as a list - wrong way", ((string[])(null)));
#line 217
  this.ScenarioSetup(scenarioInfo);
#line 218
    testRunner.Given("init: CREATE (a:A)-[:REL {value: 1}]->(b:B)-[:REL {value: 2}]->(e:End)", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 219
    testRunner.When("running: MATCH (a)-[r:REL*2..2]->(b:End) RETURN r", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table30 = new TechTalk.SpecFlow.Table(new string[] {
                        "r"});
            table30.AddRow(new string[] {
                        "[[:REL {\"value\": 1}], [:REL {\"value\": 2}]]"});
#line 220
    testRunner.Then("result:", ((string)(null)), table30, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "MatchAcceptanceTest")]
        [Xunit.TraitAttribute("Description", "should return relationships by collecting them as a list - undirected")]
        public virtual void ShouldReturnRelationshipsByCollectingThemAsAList_Undirected()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("should return relationships by collecting them as a list - undirected", ((string[])(null)));
#line 224
  this.ScenarioSetup(scenarioInfo);
#line 225
    testRunner.Given("init: CREATE (a:End {value: 1})-[:REL {value: 1}]->(b:B)-[:REL {value: 2}]->(c:En" +
                    "d {value : 2})", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 226
    testRunner.When("running: MATCH (a)-[r:REL*2..2]-(b:End) RETURN r", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table31 = new TechTalk.SpecFlow.Table(new string[] {
                        "r"});
            table31.AddRow(new string[] {
                        "[[:REL {\"value\": 1}], [:REL {\"value\": 2}]]"});
            table31.AddRow(new string[] {
                        "[[:REL {\"value\": 2}], [:REL {\"value\": 1}]]"});
#line 227
    testRunner.Then("result:", ((string)(null)), table31, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "MatchAcceptanceTest")]
        [Xunit.TraitAttribute("Description", "should return relationships by collecting them as a list")]
        public virtual void ShouldReturnRelationshipsByCollectingThemAsAList()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("should return relationships by collecting them as a list", ((string[])(null)));
#line 231
  this.ScenarioSetup(scenarioInfo);
#line 233
    testRunner.Given("init: CREATE (s:Start)-[:REL {value: 1}]->(b:B)-[:REL {value: 2}]->(c:C)", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 234
    testRunner.When("running: MATCH (a:Start)-[r:REL*2..2]-(b) RETURN r", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table32 = new TechTalk.SpecFlow.Table(new string[] {
                        "r"});
            table32.AddRow(new string[] {
                        "[[:REL {\"value\": 1}], [:REL {\"value\": 2}]]"});
#line 235
    testRunner.Then("result:", ((string)(null)), table32, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "MatchAcceptanceTest")]
        [Xunit.TraitAttribute("Description", "should return a var length path")]
        public virtual void ShouldReturnAVarLengthPath()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("should return a var length path", ((string[])(null)));
#line 239
  this.ScenarioSetup(scenarioInfo);
#line 240
    testRunner.Given("init: CREATE (a:A {name: \"A\"})-[:KNOWS {value: 1}]->(b:B {name: \"B\"})-[:KNOWS {va" +
                    "lue: 2}]->(c:C {name: \"C\"})", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 241
    testRunner.When("running: MATCH p=(n {name:\'A\'})-[:KNOWS*1..2]->(x) RETURN p", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table33 = new TechTalk.SpecFlow.Table(new string[] {
                        "p"});
            table33.AddRow(new string[] {
                        "<(:A {\"name\": \"A\"})-[:KNOWS {\"value\": 1}]->(:B {\"name\": \"B\"})>"});
            table33.AddRow(new string[] {
                        "<(:A {\"name\": \"A\"})-[:KNOWS {\"value\": 1}]->(:B {\"name\": \"B\"})-[:KNOWS {\"value\": 2" +
                            "}]->(:C {\"name\": \"C\"})>"});
#line 242
    testRunner.Then("result:", ((string)(null)), table33, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "MatchAcceptanceTest")]
        [Xunit.TraitAttribute("Description", "a var length path of length zero")]
        public virtual void AVarLengthPathOfLengthZero()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("a var length path of length zero", ((string[])(null)));
#line 247
  this.ScenarioSetup(scenarioInfo);
#line 248
    testRunner.Given("init: CREATE (a:A)-[:REL]->(b:B)", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 249
    testRunner.When("running: MATCH p=(a)-[*0..1]->(b) RETURN a,b, length(p) AS l", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table34 = new TechTalk.SpecFlow.Table(new string[] {
                        "a",
                        "b",
                        "l"});
            table34.AddRow(new string[] {
                        "(:A)",
                        "(:A)",
                        "0"});
            table34.AddRow(new string[] {
                        "(:B)",
                        "(:B)",
                        "0"});
            table34.AddRow(new string[] {
                        "(:A)",
                        "(:B)",
                        "1"});
#line 250
    testRunner.Then("result:", ((string)(null)), table34, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "MatchAcceptanceTest")]
        [Xunit.TraitAttribute("Description", "a named var length path of length zero")]
        public virtual void ANamedVarLengthPathOfLengthZero()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("a named var length path of length zero", ((string[])(null)));
#line 256
  this.ScenarioSetup(scenarioInfo);
#line 257
    testRunner.Given("init: CREATE (a:A {name: \"A\"})-[:KNOWS]->(b:B {name: \"B\"})-[:FRIEND]->(c:C {name:" +
                    " \"C\"})", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 258
    testRunner.When("running: MATCH p=(a {name:\'A\'})-[:KNOWS*0..1]->(b)-[:FRIEND*0..1]->(c) RETURN p", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table35 = new TechTalk.SpecFlow.Table(new string[] {
                        "p"});
            table35.AddRow(new string[] {
                        "<(:A {\"name\": \"A\"})>"});
            table35.AddRow(new string[] {
                        "<(:A {\"name\": \"A\"})-[:KNOWS]->(:B {\"name\": \"B\"})>"});
            table35.AddRow(new string[] {
                        "<(:A {\"name\": \"A\"})-[:KNOWS]->(:B {\"name\": \"B\"})-[:FRIEND]->(:C {\"name\": \"C\"})>"});
#line 259
    testRunner.Then("result:", ((string)(null)), table35, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [System.CodeDom.Compiler.GeneratedCodeAttribute("TechTalk.SpecFlow", "2.0.0.0")]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
        public class FixtureData : System.IDisposable
        {
            
            public FixtureData()
            {
                MatchAcceptanceTestFeature.FeatureSetup();
            }
            
            void System.IDisposable.Dispose()
            {
                MatchAcceptanceTestFeature.FeatureTearDown();
            }
        }
    }
}
#pragma warning restore
#endregion
