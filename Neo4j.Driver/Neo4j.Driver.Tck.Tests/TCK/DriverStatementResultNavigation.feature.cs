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
    [Xunit.TraitAttribute("Category", "streaming_and_cursor_navigation")]
    [Xunit.TraitAttribute("Category", "reset_database")]
    [Xunit.TraitAttribute("Category", "in_dev")]
    public partial class StatementResultNavigationFeature : Xunit.IClassFixture<StatementResultNavigationFeature.FixtureData>, System.IDisposable
    {
        
        private static TechTalk.SpecFlow.ITestRunner testRunner;
        
#line 1 "DriverStatementResultNavigation.feature"
#line hidden
        
        public StatementResultNavigationFeature()
        {
            this.TestInitialize();
        }
        
        public static void FeatureSetup()
        {
            testRunner = TechTalk.SpecFlow.TestRunnerManager.GetTestRunner();
            TechTalk.SpecFlow.FeatureInfo featureInfo = new TechTalk.SpecFlow.FeatureInfo(new System.Globalization.CultureInfo("en-US"), "Statement Result Navigation", "  This Feature is purposed to defined the uniform API of the Statement Result. Wh" +
                    "at methods that are suppose to\r\n  coexist in all the drivers and what is expecte" +
                    "d from them.", ProgrammingLanguage.CSharp, new string[] {
                        "streaming_and_cursor_navigation",
                        "reset_database",
                        "in_dev"});
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
        
        public virtual void SetFixture(StatementResultNavigationFeature.FixtureData fixtureData)
        {
        }
        
        void System.IDisposable.Dispose()
        {
            this.ScenarioTearDown();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "Statement Result Navigation")]
        [Xunit.TraitAttribute("Description", "Get single value from single Statement Result should give a record")]
        public virtual void GetSingleValueFromSingleStatementResultShouldGiveARecord()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Get single value from single Statement Result should give a record", ((string[])(null)));
#line 7
  this.ScenarioSetup(scenarioInfo);
#line 8
    testRunner.Given("init: CREATE (x:X), (x)-[:T]->(), (x)-[:T]->(), (x)-[:T]->(), (x)-[:AFFE]->();", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 9
    testRunner.When("running: MATCH (a:X) RETURN length((a)-[:T]->()) as length;", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table1 = new TechTalk.SpecFlow.Table(new string[] {
                        "length"});
            table1.AddRow(new string[] {
                        "3"});
#line 10
    testRunner.Then("using `Single` on `Statement Result` gives a `Record` containing:", ((string)(null)), table1, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "Statement Result Navigation")]
        [Xunit.TraitAttribute("Description", "Get single value from not single Statement Result should throw exception")]
        public virtual void GetSingleValueFromNotSingleStatementResultShouldThrowException()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Get single value from not single Statement Result should throw exception", ((string[])(null)));
#line 14
  this.ScenarioSetup(scenarioInfo);
#line 15
    testRunner.Given("init: CREATE ({value: 1}), ({value: 2}), ({value: 3});", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 16
    testRunner.When("running: MATCH (n), (m) RETURN n.value AS n, m.value AS m;", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table2 = new TechTalk.SpecFlow.Table(new string[] {
                        "NoSuchRecordException"});
            table2.AddRow(new string[] {
                        "Expected a result with a single record, but this result contains at least one mor" +
                            "e."});
#line 17
    testRunner.Then("using `Single` on `Statement Result` throws exception:", ((string)(null)), table2, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "Statement Result Navigation")]
        [Xunit.TraitAttribute("Description", "Get single value from empty Statement Result should throw exception")]
        public virtual void GetSingleValueFromEmptyStatementResultShouldThrowException()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Get single value from empty Statement Result should throw exception", ((string[])(null)));
#line 21
  this.ScenarioSetup(scenarioInfo);
#line 22
    testRunner.Given("running: CREATE ({value: 1}), ({value: 2}), ({value: 3});", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line hidden
            TechTalk.SpecFlow.Table table3 = new TechTalk.SpecFlow.Table(new string[] {
                        "NoSuchRecordException"});
            table3.AddRow(new string[] {
                        "Cannot retrieve the first record, because this result is empty."});
#line 23
    testRunner.Then("using `Single` on `Statement Result` throws exception:", ((string)(null)), table3, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "Statement Result Navigation")]
        [Xunit.TraitAttribute("Description", "Get single value after iterating single Statement Result should not throw excepti" +
            "on")]
        public virtual void GetSingleValueAfterIteratingSingleStatementResultShouldNotThrowException()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Get single value after iterating single Statement Result should not throw excepti" +
                    "on", ((string[])(null)));
#line 27
  this.ScenarioSetup(scenarioInfo);
#line 28
    testRunner.Given("init: CREATE ({value: 2}), ({value: 2});", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 29
    testRunner.When("running: MATCH (n) RETURN n.value AS n;", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line 30
    testRunner.And("using `Next` on `Statement Result` gives a `Record`", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
            TechTalk.SpecFlow.Table table4 = new TechTalk.SpecFlow.Table(new string[] {
                        "length"});
            table4.AddRow(new string[] {
                        "2"});
#line 31
    testRunner.Then("using `Single` on `Statement Result` gives a `Record` containing:", ((string)(null)), table4, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "Statement Result Navigation")]
        [Xunit.TraitAttribute("Description", "Native support for iterating through Statement Result")]
        public virtual void NativeSupportForIteratingThroughStatementResult()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Native support for iterating through Statement Result", ((string[])(null)));
#line 36
  this.ScenarioSetup(scenarioInfo);
#line 37
    testRunner.Given("init: CREATE ({value: 1}), ({value: 2}), ({value: 3});", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 38
    testRunner.When("running: MATCH (n), (m) RETURN n.value AS n, m.value AS m;", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line 39
    testRunner.Then("iterating through the `Statement Result` should follow the native code pattern", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "Statement Result Navigation")]
        [Xunit.TraitAttribute("Description", "Iterating through Statement Result")]
        public virtual void IteratingThroughStatementResult()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Iterating through Statement Result", ((string[])(null)));
#line 41
  this.ScenarioSetup(scenarioInfo);
#line 42
    testRunner.Given("init: CREATE (x:X), (x)-[:T]->(), (x)-[:T]->(), (x)-[:T]->(), (x)-[:AFFE]->();", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 43
    testRunner.When("running: MATCH (a:X) RETURN length((a)-[:T]->()) as length;", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table5 = new TechTalk.SpecFlow.Table(new string[] {
                        "length"});
            table5.AddRow(new string[] {
                        "3"});
#line 44
    testRunner.Then("using `Peek` on `Statement Result` gives a `Record` containing:", ((string)(null)), table5, "Then ");
#line hidden
            TechTalk.SpecFlow.Table table6 = new TechTalk.SpecFlow.Table(new string[] {
                        "length"});
            table6.AddRow(new string[] {
                        "3"});
#line 47
    testRunner.And("using `Next` on `Statement Result` gives a `Record` containing:", ((string)(null)), table6, "And ");
#line 50
    testRunner.Then("using `Peek` on `Statement Result` gives null", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Then ");
#line 51
    testRunner.And("using `Next` on `Statement Result` gives null", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "Statement Result Navigation")]
        [Xunit.TraitAttribute("Description", "Iterating through empty Statement Result")]
        public virtual void IteratingThroughEmptyStatementResult()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Iterating through empty Statement Result", ((string[])(null)));
#line 53
  this.ScenarioSetup(scenarioInfo);
#line 54
    testRunner.Given("running: MATCH (a:X) RETURN length((a)-[:T]->()) as length;", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 55
    testRunner.Then("using `Peek` on `Statement Result` gives null", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Then ");
#line 56
    testRunner.And("using `Next` on `Statement Result` gives null", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "Statement Result Navigation")]
        [Xunit.TraitAttribute("Description", "Iterating through Statement Result should be one directed")]
        public virtual void IteratingThroughStatementResultShouldBeOneDirected()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Iterating through Statement Result should be one directed", ((string[])(null)));
#line 58
  this.ScenarioSetup(scenarioInfo);
#line 59
    testRunner.Given("init: CREATE ({value: 2}), ({value: 2});", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 60
    testRunner.When("running: MATCH (n) RETURN n.value AS n;", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line 61
    testRunner.And("using `Next` on `Statement Result` gives a `Record`", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line 62
    testRunner.And("using `Next` on `Statement Result` gives a `Record`", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line 63
    testRunner.Then("it is not possible to go back", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "Statement Result Navigation")]
        [Xunit.TraitAttribute("Description", "Get keys from Statement Result")]
        public virtual void GetKeysFromStatementResult()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Get keys from Statement Result", ((string[])(null)));
#line 65
  this.ScenarioSetup(scenarioInfo);
#line 66
    testRunner.Given("init: CREATE (a:A {value: 1})-[:REL {name: \"r1\"}]->(b:B {value: 2})-[:REL {name: " +
                    "\"r2\"}]->(c:C {value: 3})", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 67
    testRunner.When("running: MATCH (a)-[r {name:\'r1\'}]-(b) OPTIONAL MATCH (b)-[r2]-(c) WHERE r<>r2 RE" +
                    "TURN a,b,c", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table7 = new TechTalk.SpecFlow.Table(new string[] {
                        "keys"});
            table7.AddRow(new string[] {
                        "a"});
            table7.AddRow(new string[] {
                        "b"});
            table7.AddRow(new string[] {
                        "c"});
#line 68
    testRunner.Then("using `Keys` on `Statement Result` gives:", ((string)(null)), table7, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "Statement Result Navigation")]
        [Xunit.TraitAttribute("Description", "Get keys from empty Statement Result")]
        public virtual void GetKeysFromEmptyStatementResult()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Get keys from empty Statement Result", ((string[])(null)));
#line 74
  this.ScenarioSetup(scenarioInfo);
#line 75
    testRunner.Given("running: CREATE (x:X), (x)-[:T]->(), (x)-[:T]->(), (x)-[:T]->(), (x)-[:AFFE]->();" +
                    "", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line hidden
            TechTalk.SpecFlow.Table table8 = new TechTalk.SpecFlow.Table(new string[] {
                        "keys"});
#line 76
    testRunner.Then("using `Keys` on `Statement Result` gives:", ((string)(null)), table8, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "Statement Result Navigation")]
        [Xunit.TraitAttribute("Description", "Get list from Statement Result")]
        public virtual void GetListFromStatementResult()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Get list from Statement Result", ((string[])(null)));
#line 79
  this.ScenarioSetup(scenarioInfo);
#line 80
    testRunner.Given("init: CREATE ({value: 1}), ({value: 2}), ({value: 3});", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 81
    testRunner.When("running: MATCH (n), (m) RETURN n.value AS n, m.value AS m;", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table9 = new TechTalk.SpecFlow.Table(new string[] {
                        "n",
                        "m"});
            table9.AddRow(new string[] {
                        "1",
                        "1"});
            table9.AddRow(new string[] {
                        "1",
                        "2"});
            table9.AddRow(new string[] {
                        "1",
                        "3"});
            table9.AddRow(new string[] {
                        "2",
                        "1"});
            table9.AddRow(new string[] {
                        "2",
                        "2"});
            table9.AddRow(new string[] {
                        "2",
                        "3"});
            table9.AddRow(new string[] {
                        "3",
                        "3"});
            table9.AddRow(new string[] {
                        "3",
                        "1"});
            table9.AddRow(new string[] {
                        "3",
                        "2"});
#line 82
    testRunner.Then("using `List` on `Statement Result` gives:", ((string)(null)), table9, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "Statement Result Navigation")]
        [Xunit.TraitAttribute("Description", "Get list from empty Statement Result")]
        public virtual void GetListFromEmptyStatementResult()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Get list from empty Statement Result", ((string[])(null)));
#line 94
  this.ScenarioSetup(scenarioInfo);
#line 95
    testRunner.Given("running: CREATE (x:X), (x)-[:T]->(), (x)-[:T]->(), (x)-[:T]->(), (x)-[:AFFE]->();" +
                    "", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line hidden
            TechTalk.SpecFlow.Table table10 = new TechTalk.SpecFlow.Table(new string[] {
                        ""});
#line 96
    testRunner.Then("using `List` on `Statement Result` gives:", ((string)(null)), table10, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "Statement Result Navigation")]
        [Xunit.TraitAttribute("Description", "List on Statement Result should give the remaining records")]
        public virtual void ListOnStatementResultShouldGiveTheRemainingRecords()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("List on Statement Result should give the remaining records", ((string[])(null)));
#line 99
  this.ScenarioSetup(scenarioInfo);
#line 100
    testRunner.Given("init: CREATE ({value: 1}), ({value: 2}), ({value: 3});", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 101
    testRunner.When("running: MATCH (n), (m) RETURN n.value AS n, m.value AS m;", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line 102
    testRunner.And("using `Next` on `Statement Result` gives a `Record`", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line 103
    testRunner.And("using `Next` on `Statement Result` gives a `Record`", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line 104
    testRunner.Then("it is not possible to go back", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Then ");
#line 105
    testRunner.And("using `List` on `Statement Result` gives a list of size 7, the previous records a" +
                    "re lost", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "Statement Result Navigation")]
        [Xunit.TraitAttribute("Description", "Record should contain key")]
        public virtual void RecordShouldContainKey()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Record should contain key", ((string[])(null)));
#line 107
  this.ScenarioSetup(scenarioInfo);
#line 108
    testRunner.Given("init: CREATE (a:A {value : 1})-[:KNOWS]->(b:B {value : 2})", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 109
    testRunner.When("running: MATCH (n1)-[rel:KNOWS]->(n2) RETURN n1, n2", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table11 = new TechTalk.SpecFlow.Table(new string[] {
                        "keys"});
            table11.AddRow(new string[] {
                        "n1"});
            table11.AddRow(new string[] {
                        "n2"});
#line 110
    testRunner.Then("using `Keys` on the single record gives:", ((string)(null)), table11, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "Statement Result Navigation")]
        [Xunit.TraitAttribute("Description", "Record should contain value")]
        public virtual void RecordShouldContainValue()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Record should contain value", ((string[])(null)));
#line 115
  this.ScenarioSetup(scenarioInfo);
#line 116
    testRunner.Given("init: CREATE (a:A {value : 1})-[:KNOWS]->(b:B {value : 2})", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 117
    testRunner.When("running: MATCH (n1)-[rel:KNOWS]->(n2) RETURN n1, n2", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table12 = new TechTalk.SpecFlow.Table(new string[] {
                        "values"});
            table12.AddRow(new string[] {
                        "(:A {\"value\": 1})"});
            table12.AddRow(new string[] {
                        "(:B {\"value\": 2})"});
#line 118
    testRunner.Then("using `Values` on the single record gives:", ((string)(null)), table12, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "Statement Result Navigation")]
        [Xunit.TraitAttribute("Description", "Record should contain key and value and value should be retriveble by index")]
        public virtual void RecordShouldContainKeyAndValueAndValueShouldBeRetrivebleByIndex()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Record should contain key and value and value should be retriveble by index", ((string[])(null)));
#line 123
  this.ScenarioSetup(scenarioInfo);
#line 124
    testRunner.Given("init: CREATE (a:A {value : 1})-[:KNOWS]->(b:B {value : 2})", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 125
    testRunner.When("running: MATCH (n1)-[rel:KNOWS]->(n2) RETURN n1, n2", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table13 = new TechTalk.SpecFlow.Table(new string[] {
                        "value"});
            table13.AddRow(new string[] {
                        "(:A {\"value\": 1})"});
#line 126
    testRunner.Then("using `Get` with index 0 on the single record gives:", ((string)(null)), table13, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "Statement Result Navigation")]
        [Xunit.TraitAttribute("Description", "Record should contain key and value and value should be retriveble by key")]
        public virtual void RecordShouldContainKeyAndValueAndValueShouldBeRetrivebleByKey()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Record should contain key and value and value should be retriveble by key", ((string[])(null)));
#line 130
  this.ScenarioSetup(scenarioInfo);
#line 131
    testRunner.Given("init: CREATE (a:A {value : 1})-[:KNOWS]->(b:B {value : 2})", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 132
    testRunner.When("running: MATCH (n1)-[rel:KNOWS]->(n2) RETURN n1, n2", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table14 = new TechTalk.SpecFlow.Table(new string[] {
                        "value"});
            table14.AddRow(new string[] {
                        "(:B {\"value\": 2})"});
#line 133
    testRunner.Then("using `Get` with key `n2` on the single record gives:", ((string)(null)), table14, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [System.CodeDom.Compiler.GeneratedCodeAttribute("TechTalk.SpecFlow", "2.0.0.0")]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
        public class FixtureData : System.IDisposable
        {
            
            public FixtureData()
            {
                StatementResultNavigationFeature.FeatureSetup();
            }
            
            void System.IDisposable.Dispose()
            {
                StatementResultNavigationFeature.FeatureTearDown();
            }
        }
    }
}
#pragma warning restore
#endregion
