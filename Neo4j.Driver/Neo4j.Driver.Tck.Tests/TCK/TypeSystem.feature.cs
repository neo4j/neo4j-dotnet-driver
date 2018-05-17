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
    [Xunit.TraitAttribute("Category", "type_test")]
    public partial class DriverTypesTestEchoingSingleParameterFeature : Xunit.IClassFixture<DriverTypesTestEchoingSingleParameterFeature.FixtureData>, System.IDisposable
    {
        
        private static TechTalk.SpecFlow.ITestRunner testRunner;
        
#line 1 "TypeSystem.feature"
#line hidden
        
        public DriverTypesTestEchoingSingleParameterFeature()
        {
            this.TestInitialize();
        }
        
        public static void FeatureSetup()
        {
            testRunner = TechTalk.SpecFlow.TestRunnerManager.GetTestRunner();
            TechTalk.SpecFlow.FeatureInfo featureInfo = new TechTalk.SpecFlow.FeatureInfo(new System.Globalization.CultureInfo("en-US"), "Driver Types Test Echoing Single Parameter", @"  The following types are supported by bolt.
  | Null        | Represents the absence of a value   |
  | Boolean     | Boolean true or false |
  | Integer     | 64-bit signed integer |
  | Float       | 64-bit floating point number|
  | String      | Unicode string|
  | List        | Ordered collection of values|
  | Map         | Unordered, keyed collection of values|
  | Node        | A node in the graph with optional properties and labels|
  | Relationship| A directed, typed connection between two nodes. Each relationship may have properties and always has an identity|
  | Path        | The record of a directed walk through the graph, a sequence of zero or more segments*. A path with zero segments consists of a single node.|

  It is important that the types that are sent over Bolt are not corrupted.
  These Scenarios will echo different types and make sure that the returned object is of the same type and value as
  the one sent to the server.

  Echoing to the server can be done by using the cypher statement ""RETURN <value>"",
  or ""RETURN {value}"" with value provided via a parameter.
  It is recommended to test each supported way of sending statements that the driver provides while running these
  cucumber scenarios.", ProgrammingLanguage.CSharp, new string[] {
                        "type_test"});
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
        
        public virtual void FeatureBackground()
        {
#line 24
  #line 25
    testRunner.Given("A running database", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line hidden
        }
        
        public virtual void SetFixture(DriverTypesTestEchoingSingleParameterFeature.FixtureData fixtureData)
        {
        }
        
        void System.IDisposable.Dispose()
        {
            this.ScenarioTearDown();
        }
        
        [Xunit.TheoryAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "Driver Types Test Echoing Single Parameter")]
        [Xunit.TraitAttribute("Description", "should return the same type and value")]
        [Xunit.InlineDataAttribute("Null", "null", new string[0])]
        [Xunit.InlineDataAttribute("Boolean", "true", new string[0])]
        [Xunit.InlineDataAttribute("Boolean", "false", new string[0])]
        [Xunit.InlineDataAttribute("Integer", "1", new string[0])]
        [Xunit.InlineDataAttribute("Integer", "-17", new string[0])]
        [Xunit.InlineDataAttribute("Integer", "-129", new string[0])]
        [Xunit.InlineDataAttribute("Integer", "129", new string[0])]
        [Xunit.InlineDataAttribute("Integer", "2147483647", new string[0])]
        [Xunit.InlineDataAttribute("Integer", "-2147483648", new string[0])]
        [Xunit.InlineDataAttribute("Integer", "9223372036854775807", new string[0])]
        [Xunit.InlineDataAttribute("Integer", "-9223372036854775808", new string[0])]
        [Xunit.InlineDataAttribute("Float", "1.7976931348623157E+308", new string[0])]
        [Xunit.InlineDataAttribute("Float", "2.2250738585072014e-308", new string[0])]
        [Xunit.InlineDataAttribute("Float", "4.9E-324", new string[0])]
        [Xunit.InlineDataAttribute("Float", "0", new string[0])]
        [Xunit.InlineDataAttribute("Float", "1.1", new string[0])]
        [Xunit.InlineDataAttribute("String", "1", new string[0])]
        [Xunit.InlineDataAttribute("String", "-17∂ßå®", new string[0])]
        [Xunit.InlineDataAttribute("String", "String", new string[0])]
        [Xunit.InlineDataAttribute("String", "", new string[0])]
        public virtual void ShouldReturnTheSameTypeAndValue(string boltType, string input, string[] exampleTags)
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("should return the same type and value", exampleTags);
#line 27
  this.ScenarioSetup(scenarioInfo);
#line 24
  this.FeatureBackground();
#line 28
    testRunner.Given(string.Format("a value {0} of type {1}", input, boltType), ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 29
    testRunner.When("the driver asks the server to echo this value back", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line 30
    testRunner.And("the value given in the result should be the same as what was sent", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.TheoryAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "Driver Types Test Echoing Single Parameter")]
        [Xunit.TraitAttribute("Description", "Should echo list")]
        [Xunit.InlineDataAttribute("Integer", "[1,2,3,4]", new string[0])]
        [Xunit.InlineDataAttribute("Boolean", "[true,false]", new string[0])]
        [Xunit.InlineDataAttribute("Float", "[1.1,2.2,3.3]", new string[0])]
        [Xunit.InlineDataAttribute("String", "[a,b,c,˚C]", new string[0])]
        [Xunit.InlineDataAttribute("Null", "[null, null]", new string[0])]
        public virtual void ShouldEchoList(string boltType, string input, string[] exampleTags)
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Should echo list", exampleTags);
#line 73
  this.ScenarioSetup(scenarioInfo);
#line 24
  this.FeatureBackground();
#line 74
    testRunner.Given(string.Format("a list value {0} of type {1}", input, boltType), ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 75
    testRunner.When("the driver asks the server to echo this value back", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line 76
    testRunner.And("the value given in the result should be the same as what was sent", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "Driver Types Test Echoing Single Parameter")]
        [Xunit.TraitAttribute("Description", "Should echo list of lists, maps and values")]
        public virtual void ShouldEchoListOfListsMapsAndValues()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Should echo list of lists, maps and values", ((string[])(null)));
#line 85
  this.ScenarioSetup(scenarioInfo);
#line 24
  this.FeatureBackground();
#line 86
    testRunner.Given("an empty list L", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line hidden
            TechTalk.SpecFlow.Table table1 = new TechTalk.SpecFlow.Table(new string[] {
                        "Integer",
                        "[1,2,3,4]"});
            table1.AddRow(new string[] {
                        "Boolean",
                        "[true,true]"});
            table1.AddRow(new string[] {
                        "Float",
                        "[1.1,2.2,3.3]"});
            table1.AddRow(new string[] {
                        "String",
                        "[a,b,c,˚C]"});
            table1.AddRow(new string[] {
                        "Null",
                        "[null,null]"});
#line 87
    testRunner.And("adding a table of lists to the list L", ((string)(null)), table1, "And ");
#line hidden
            TechTalk.SpecFlow.Table table2 = new TechTalk.SpecFlow.Table(new string[] {
                        "Integer",
                        "1"});
            table2.AddRow(new string[] {
                        "Boolean",
                        "true"});
            table2.AddRow(new string[] {
                        "Float",
                        "1.1"});
            table2.AddRow(new string[] {
                        "String",
                        "˚C"});
            table2.AddRow(new string[] {
                        "Null",
                        "null"});
#line 93
    testRunner.And("adding a table of values to the list L", ((string)(null)), table2, "And ");
#line 99
    testRunner.And("an empty map M", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
            TechTalk.SpecFlow.Table table3 = new TechTalk.SpecFlow.Table(new string[] {
                        "Integer",
                        "1"});
            table3.AddRow(new string[] {
                        "Boolean",
                        "true"});
            table3.AddRow(new string[] {
                        "Float",
                        "1.1"});
            table3.AddRow(new string[] {
                        "String",
                        "˚C"});
            table3.AddRow(new string[] {
                        "Null",
                        "null"});
#line 100
    testRunner.And("adding a table of values to the map M", ((string)(null)), table3, "And ");
#line 106
    testRunner.And("adding map M to list L", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line 107
    testRunner.When("the driver asks the server to echo this list back", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line 108
    testRunner.And("the value given in the result should be the same as what was sent", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "Driver Types Test Echoing Single Parameter")]
        [Xunit.TraitAttribute("Description", "Should echo map")]
        public virtual void ShouldEchoMap()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Should echo map", ((string[])(null)));
#line 110
  this.ScenarioSetup(scenarioInfo);
#line 24
  this.FeatureBackground();
#line 111
    testRunner.Given("an empty map M", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line hidden
            TechTalk.SpecFlow.Table table4 = new TechTalk.SpecFlow.Table(new string[] {
                        "Integer",
                        "[1,2,3,4]"});
            table4.AddRow(new string[] {
                        "Boolean",
                        "[true,true]"});
            table4.AddRow(new string[] {
                        "Float",
                        "[1.1,2.2,3.3]"});
            table4.AddRow(new string[] {
                        "String",
                        "[a,b,c,˚C]"});
            table4.AddRow(new string[] {
                        "Null",
                        "[null,null]"});
#line 112
    testRunner.When("adding a table of lists to the map M", ((string)(null)), table4, "When ");
#line hidden
            TechTalk.SpecFlow.Table table5 = new TechTalk.SpecFlow.Table(new string[] {
                        "Integer",
                        "1"});
            table5.AddRow(new string[] {
                        "Boolean",
                        "true"});
            table5.AddRow(new string[] {
                        "Float",
                        "1.1"});
            table5.AddRow(new string[] {
                        "String",
                        "˚C"});
            table5.AddRow(new string[] {
                        "Null",
                        "null"});
#line 118
    testRunner.And("adding a table of values to the map M", ((string)(null)), table5, "And ");
#line 124
    testRunner.And("adding a copy of map M to map M", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line 125
    testRunner.When("the driver asks the server to echo this map back", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line 126
    testRunner.And("the value given in the result should be the same as what was sent", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [System.CodeDom.Compiler.GeneratedCodeAttribute("TechTalk.SpecFlow", "2.0.0.0")]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
        public class FixtureData : System.IDisposable
        {
            
            public FixtureData()
            {
                DriverTypesTestEchoingSingleParameterFeature.FeatureSetup();
            }
            
            void System.IDisposable.Dispose()
            {
                DriverTypesTestEchoingSingleParameterFeature.FeatureTearDown();
            }
        }
    }
}
#pragma warning restore
#endregion
