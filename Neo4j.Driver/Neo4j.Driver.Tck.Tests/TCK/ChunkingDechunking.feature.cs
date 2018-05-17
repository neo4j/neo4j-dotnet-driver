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
    [Xunit.TraitAttribute("Category", "chunking_dechunking")]
    public partial class DriverChunkingAndDe_ChunkingTestFeature : Xunit.IClassFixture<DriverChunkingAndDe_ChunkingTestFeature.FixtureData>, System.IDisposable
    {
        
        private static TechTalk.SpecFlow.ITestRunner testRunner;
        
#line 1 "ChunkingDechunking.feature"
#line hidden
        
        public DriverChunkingAndDe_ChunkingTestFeature()
        {
            this.TestInitialize();
        }
        
        public static void FeatureSetup()
        {
            testRunner = TechTalk.SpecFlow.TestRunnerManager.GetTestRunner();
            TechTalk.SpecFlow.FeatureInfo featureInfo = new TechTalk.SpecFlow.FeatureInfo(new System.Globalization.CultureInfo("en-US"), "Driver Chunking and De-chunking Test", @"  It is important that the types that are sent over Bolt are not corrupted.
  These Scenarios will send very large types or collections of types so that Bolts chunking and dechunking
  functionality is used.
  Similar to to the type system feature scenarios these scenarios will echo these large values and make sure that the
  returning values are the same.

  Echoing to the server can be done by using the cypher statement ""RETURN <value>"",
  or ""RETURN {value}"" with value provided via a parameter.
  It is recommended to test each supported way of sending statements that the driver provides while running these
  cucumber scenarios.", ProgrammingLanguage.CSharp, new string[] {
                        "chunking_dechunking"});
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
        
        public virtual void SetFixture(DriverChunkingAndDe_ChunkingTestFeature.FixtureData fixtureData)
        {
        }
        
        void System.IDisposable.Dispose()
        {
            this.ScenarioTearDown();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "Driver Chunking and De-chunking Test")]
        [Xunit.TraitAttribute("Description", "should echo very long string")]
        public virtual void ShouldEchoVeryLongString()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("should echo very long string", ((string[])(null)));
#line 15
  this.ScenarioSetup(scenarioInfo);
#line 16
    testRunner.Given("a String of size 10000", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 17
    testRunner.When("the driver asks the server to echo this value back", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line 18
    testRunner.And("the value given in the result should be the same as what was sent", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.TheoryAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "Driver Chunking and De-chunking Test")]
        [Xunit.TraitAttribute("Description", "should echo very long list")]
        [Xunit.InlineDataAttribute("Null", new string[0])]
        [Xunit.InlineDataAttribute("Boolean", new string[0])]
        [Xunit.InlineDataAttribute("Integer", new string[0])]
        [Xunit.InlineDataAttribute("Float", new string[0])]
        [Xunit.InlineDataAttribute("String", new string[0])]
        public virtual void ShouldEchoVeryLongList(string type, string[] exampleTags)
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("should echo very long list", exampleTags);
#line 20
  this.ScenarioSetup(scenarioInfo);
#line 21
    testRunner.Given(string.Format("a List of size 1000 and type {0}", type), ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 22
    testRunner.When("the driver asks the server to echo this value back", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line 23
    testRunner.And("the value given in the result should be the same as what was sent", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.TheoryAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "Driver Chunking and De-chunking Test")]
        [Xunit.TraitAttribute("Description", "should echo very long map")]
        [Xunit.InlineDataAttribute("Null", new string[0])]
        [Xunit.InlineDataAttribute("Boolean", new string[0])]
        [Xunit.InlineDataAttribute("Integer", new string[0])]
        [Xunit.InlineDataAttribute("Float", new string[0])]
        [Xunit.InlineDataAttribute("String", new string[0])]
        public virtual void ShouldEchoVeryLongMap(string type, string[] exampleTags)
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("should echo very long map", exampleTags);
#line 32
  this.ScenarioSetup(scenarioInfo);
#line 33
    testRunner.Given(string.Format("a Map of size 1000 and type {0}", type), ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 34
    testRunner.When("the driver asks the server to echo this value back", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line 35
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
                DriverChunkingAndDe_ChunkingTestFeature.FeatureSetup();
            }
            
            void System.IDisposable.Dispose()
            {
                DriverChunkingAndDe_ChunkingTestFeature.FeatureTearDown();
            }
        }
    }
}
#pragma warning restore
#endregion
