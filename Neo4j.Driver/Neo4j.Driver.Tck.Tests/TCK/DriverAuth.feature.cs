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
    [Xunit.TraitAttribute("Category", "auth")]
    public partial class AuthenticationForDriversFeature : Xunit.IClassFixture<AuthenticationForDriversFeature.FixtureData>, System.IDisposable
    {
        
        private static TechTalk.SpecFlow.ITestRunner testRunner;
        
#line 1 "DriverAuth.feature"
#line hidden
        
        public AuthenticationForDriversFeature()
        {
            this.TestInitialize();
        }
        
        public static void FeatureSetup()
        {
            testRunner = TechTalk.SpecFlow.TestRunnerManager.GetTestRunner();
            TechTalk.SpecFlow.FeatureInfo featureInfo = new TechTalk.SpecFlow.FeatureInfo(new System.Globalization.CultureInfo("en-US"), "Authentication for drivers", null, ProgrammingLanguage.CSharp, new string[] {
                        "auth"});
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
        
        public virtual void SetFixture(AuthenticationForDriversFeature.FixtureData fixtureData)
        {
        }
        
        void System.IDisposable.Dispose()
        {
            this.ScenarioTearDown();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "Authentication for drivers")]
        [Xunit.TraitAttribute("Description", "Should be able to start and run against database with driver auth enabled and cor" +
            "rect password is provided")]
        public virtual void ShouldBeAbleToStartAndRunAgainstDatabaseWithDriverAuthEnabledAndCorrectPasswordIsProvided()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Should be able to start and run against database with driver auth enabled and cor" +
                    "rect password is provided", ((string[])(null)));
#line 4
  this.ScenarioSetup(scenarioInfo);
#line 5
    testRunner.Given("a driver is configured with auth enabled and correct password is provided", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 6
    testRunner.Then("reading and writing to the database should be possible", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "Authentication for drivers")]
        [Xunit.TraitAttribute("Description", "Should not be able to start and run against database with driver auth enabled and" +
            " wrong password is provided")]
        public virtual void ShouldNotBeAbleToStartAndRunAgainstDatabaseWithDriverAuthEnabledAndWrongPasswordIsProvided()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Should not be able to start and run against database with driver auth enabled and" +
                    " wrong password is provided", ((string[])(null)));
#line 8
  this.ScenarioSetup(scenarioInfo);
#line 9
    testRunner.Given("a driver is configured with auth enabled and the wrong password is provided", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 10
    testRunner.Then("reading and writing to the database should not be possible", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Then ");
#line 11
    testRunner.And("a `Protocol Error` is raised", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [System.CodeDom.Compiler.GeneratedCodeAttribute("TechTalk.SpecFlow", "2.0.0.0")]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
        public class FixtureData : System.IDisposable
        {
            
            public FixtureData()
            {
                AuthenticationForDriversFeature.FeatureSetup();
            }
            
            void System.IDisposable.Dispose()
            {
                AuthenticationForDriversFeature.FeatureTearDown();
            }
        }
    }
}
#pragma warning restore
#endregion
