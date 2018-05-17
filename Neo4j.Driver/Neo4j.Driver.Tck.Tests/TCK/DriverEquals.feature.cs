// Copyright (c) 2002-2018 "Neo4j,"
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
#region Designer generated code
#pragma warning disable
namespace Neo4j.Driver.Tck.Tests.TCK
{
    using TechTalk.SpecFlow;
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("TechTalk.SpecFlow", "2.0.0.0")]
    [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [Xunit.TraitAttribute("Category", "equality_test")]
    [Xunit.TraitAttribute("Category", "reset_database")]
    public partial class DriverEqualsFeatureFeature : Xunit.IClassFixture<DriverEqualsFeatureFeature.FixtureData>, System.IDisposable
    {
        
        private static TechTalk.SpecFlow.ITestRunner testRunner;
        
#line 1 "DriverEquals.feature"
#line hidden
        
        public DriverEqualsFeatureFeature()
        {
            this.TestInitialize();
        }
        
        public static void FeatureSetup()
        {
            testRunner = TechTalk.SpecFlow.TestRunnerManager.GetTestRunner();
            TechTalk.SpecFlow.FeatureInfo featureInfo = new TechTalk.SpecFlow.FeatureInfo(new System.Globalization.CultureInfo("en-US"), "Driver Equals Feature", @"  Comparing Nodes, Relationship and Path should not bother with the content of those objects but rather compare if these
  are the same object.

  Nodes and Relationships

  Nodes and Relationships should compare IDs only. This means that the content may differ since it may mean comparing
  these at different times.

  Paths
  Paths need to compare the IDs of the Nodes and Relationships inside the path as well as the start and end values of
  the relationships. A path is only equal if it is the same path containing the same elements. ", ProgrammingLanguage.CSharp, new string[] {
                        "equality_test",
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
        
        public virtual void SetFixture(DriverEqualsFeatureFeature.FixtureData fixtureData)
        {
        }
        
        void System.IDisposable.Dispose()
        {
            this.ScenarioTearDown();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "Driver Equals Feature")]
        [Xunit.TraitAttribute("Description", "Compare modified node")]
        public virtual void CompareModifiedNode()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Compare modified node", ((string[])(null)));
#line 16
  this.ScenarioSetup(scenarioInfo);
#line 17
    testRunner.Given("init: CREATE (:label1)", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 18
    testRunner.And("`value1` is single value result of: MATCH (n:label1) RETURN n", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line 19
    testRunner.When("running: MATCH (n:label1) SET n.foo = \'bar\' SET n :label2", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line 20
    testRunner.And("`value2` is single value result of: MATCH (n:label1) RETURN n", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line 21
    testRunner.Then("saved values should all equal", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "Driver Equals Feature")]
        [Xunit.TraitAttribute("Description", "Compare different nodes with same content")]
        public virtual void CompareDifferentNodesWithSameContent()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Compare different nodes with same content", ((string[])(null)));
#line 23
  this.ScenarioSetup(scenarioInfo);
#line 24
    testRunner.Given("`value1` is single value result of: CREATE (n:label1) RETURN n", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 25
    testRunner.And("`value2` is single value result of:  CREATE (n:label1) RETURN n", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line 26
    testRunner.Then("none of the saved values should be equal", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "Driver Equals Feature")]
        [Xunit.TraitAttribute("Description", "Compare modified Relationship")]
        public virtual void CompareModifiedRelationship()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Compare modified Relationship", ((string[])(null)));
#line 28
  this.ScenarioSetup(scenarioInfo);
#line 29
    testRunner.Given("init: CREATE (a {name: \"A\"}), (b {name: \"B\"}), (a)-[:KNOWS]->(b)", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 30
    testRunner.And("`value1` is single value result of: MATCH (n)-[r:KNOWS]->(x) RETURN r", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line 31
    testRunner.When("running: MATCH (n)-[r:KNOWS]->(x) SET r.foo = \'bar\'", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line 32
    testRunner.And("`value2` is single value result of: MATCH (n)-[r:KNOWS]->(x) RETURN r", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line 33
    testRunner.Then("saved values should all equal", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "Driver Equals Feature")]
        [Xunit.TraitAttribute("Description", "Compare different relationships with same content")]
        public virtual void CompareDifferentRelationshipsWithSameContent()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Compare different relationships with same content", ((string[])(null)));
#line 35
  this.ScenarioSetup(scenarioInfo);
#line 36
    testRunner.Given("`value1` is single value result of: CREATE (a {name: \"A\"}), (b {name: \"B\"}), (a)-" +
                    "[r:KNOWS]->(b) return r", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 37
    testRunner.And("`value2` is single value result of:  CREATE (a {name: \"A\"}), (b {name: \"B\"}), (a)" +
                    "-[r:KNOWS]->(b) return r", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line 38
    testRunner.Then("none of the saved values should be equal", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "Driver Equals Feature")]
        [Xunit.TraitAttribute("Description", "Compare modified path")]
        public virtual void CompareModifiedPath()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Compare modified path", ((string[])(null)));
#line 40
  this.ScenarioSetup(scenarioInfo);
#line 41
    testRunner.Given("init: CREATE (a:A {name: \"A\"})-[:KNOWS]->(b:B {name: \"B\"})", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 42
    testRunner.And("`value1` is single value result of: MATCH p=(a {name:\'A\'})-->(b) RETURN p", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line 43
    testRunner.When("running: MATCH (n:A {name: \"A\"}) SET n.foo = \'bar\' SET n :label2", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line 44
    testRunner.And("running: MATCH (n)-[r:KNOWS]->(x) SET r.foo = \'bar\'", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line 45
    testRunner.And("`value2` is single value result of: MATCH p=(a {name:\'A\'})-->(b) RETURN p", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line 46
    testRunner.Then("saved values should all equal", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "Driver Equals Feature")]
        [Xunit.TraitAttribute("Description", "Compare different path with same content")]
        public virtual void CompareDifferentPathWithSameContent()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Compare different path with same content", ((string[])(null)));
#line 48
  this.ScenarioSetup(scenarioInfo);
#line 49
    testRunner.Given("`value1` is single value result of: CREATE p=((a:A {name: \"A\"})-[:KNOWS]->(b:B {n" +
                    "ame: \"B\"})) RETURN p", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 50
    testRunner.And("`value2` is single value result of: CREATE p=((a:A {name: \"A\"})-[:KNOWS]->(b:B {n" +
                    "ame: \"B\"})) RETURN p", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line 51
    testRunner.Then("none of the saved values should be equal", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [System.CodeDom.Compiler.GeneratedCodeAttribute("TechTalk.SpecFlow", "2.0.0.0")]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
        public class FixtureData : System.IDisposable
        {
            
            public FixtureData()
            {
                DriverEqualsFeatureFeature.FeatureSetup();
            }
            
            void System.IDisposable.Dispose()
            {
                DriverEqualsFeatureFeature.FeatureTearDown();
            }
        }
    }
}
#pragma warning restore
#endregion
