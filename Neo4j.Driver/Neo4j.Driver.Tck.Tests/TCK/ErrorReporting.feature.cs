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
#region Designer generated code
#pragma warning disable
namespace Neo4j.Driver.Tck.Tests.TCK
{
    using TechTalk.SpecFlow;
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("TechTalk.SpecFlow", "2.0.0.0")]
    [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [Xunit.TraitAttribute("Category", "error_reporting")]
    public partial class ErrorReportingFeature : Xunit.IClassFixture<ErrorReportingFeature.FixtureData>, System.IDisposable
    {
        
        private static TechTalk.SpecFlow.ITestRunner testRunner;
        
#line 1 "ErrorReporting.feature"
#line hidden
        
        public ErrorReportingFeature()
        {
            this.TestInitialize();
        }
        
        public static void FeatureSetup()
        {
            testRunner = TechTalk.SpecFlow.TestRunnerManager.GetTestRunner();
            TechTalk.SpecFlow.FeatureInfo featureInfo = new TechTalk.SpecFlow.FeatureInfo(new System.Globalization.CultureInfo("en-US"), "Error Reporting", null, ProgrammingLanguage.CSharp, new string[] {
                        "error_reporting"});
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
        
        public virtual void SetFixture(ErrorReportingFeature.FixtureData fixtureData)
        {
        }
        
        void System.IDisposable.Dispose()
        {
            this.ScenarioTearDown();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "Error Reporting")]
        [Xunit.TraitAttribute("Description", "Running a session before closing transaction should give exception")]
        public virtual void RunningASessionBeforeClosingTransactionShouldGiveException()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Running a session before closing transaction should give exception", ((string[])(null)));
#line 4
  this.ScenarioSetup(scenarioInfo);
#line 5
    testRunner.Given("I have a driver", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 6
    testRunner.When("I start a `Transaction` through a session", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line 7
    testRunner.And("`run` a query with that same session without closing the transaction first", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
            TechTalk.SpecFlow.Table table1 = new TechTalk.SpecFlow.Table(new string[] {
                        "start of message"});
            table1.AddRow(new string[] {
                        "Please close the currently open transaction object before running more statements" +
                            "/transactions in the current session."});
#line 8
    testRunner.Then("it throws a `ClientException`", ((string)(null)), table1, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "Error Reporting")]
        [Xunit.TraitAttribute("Description", "Beginning a new transaction before closing the previous should give exception")]
        public virtual void BeginningANewTransactionBeforeClosingThePreviousShouldGiveException()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Beginning a new transaction before closing the previous should give exception", ((string[])(null)));
#line 12
  this.ScenarioSetup(scenarioInfo);
#line 13
    testRunner.Given("I have a driver", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 14
    testRunner.When("I start a `Transaction` through a session", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line 15
    testRunner.And("I start a new `Transaction` with the same session before closing the previous", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
            TechTalk.SpecFlow.Table table2 = new TechTalk.SpecFlow.Table(new string[] {
                        "start of message"});
            table2.AddRow(new string[] {
                        "Please close the currently open transaction object before running more statements" +
                            "/transactions in the current session."});
#line 16
    testRunner.Then("it throws a `ClientException`", ((string)(null)), table2, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "Error Reporting")]
        [Xunit.TraitAttribute("Description", "Misspelled cypher statement should give exception")]
        public virtual void MisspelledCypherStatementShouldGiveException()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Misspelled cypher statement should give exception", ((string[])(null)));
#line 20
  this.ScenarioSetup(scenarioInfo);
#line 21
    testRunner.Given("I have a driver", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 22
    testRunner.When("I run a non valid cypher statement", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table3 = new TechTalk.SpecFlow.Table(new string[] {
                        "start of message"});
            table3.AddRow(new string[] {
                        "Invalid input"});
#line 23
    testRunner.Then("it throws a `ClientException`", ((string)(null)), table3, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "Error Reporting")]
        [Xunit.TraitAttribute("Description", "Trying to connect a driver to the wrong port gives exception")]
        public virtual void TryingToConnectADriverToTheWrongPortGivesException()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Trying to connect a driver to the wrong port gives exception", ((string[])(null)));
#line 27
  this.ScenarioSetup(scenarioInfo);
#line 28
    testRunner.When("I set up a driver to an incorrect port", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table4 = new TechTalk.SpecFlow.Table(new string[] {
                        "start of message"});
            table4.AddRow(new string[] {
                        "Unable to connect to"});
#line 29
    testRunner.Then("it throws a `ClientException`", ((string)(null)), table4, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute()]
        [Xunit.TraitAttribute("FeatureTitle", "Error Reporting")]
        [Xunit.TraitAttribute("Description", "Trying to connect a driver to the wrong scheme gives exception")]
        public virtual void TryingToConnectADriverToTheWrongSchemeGivesException()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Trying to connect a driver to the wrong scheme gives exception", ((string[])(null)));
#line 33
  this.ScenarioSetup(scenarioInfo);
#line 34
    testRunner.When("I set up a driver with wrong scheme", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table5 = new TechTalk.SpecFlow.Table(new string[] {
                        "start of message"});
            table5.AddRow(new string[] {
                        "Unsupported transport:"});
#line 35
    testRunner.Then("it throws a `ClientException`", ((string)(null)), table5, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute(Skip="Ignored")]
        [Xunit.TraitAttribute("FeatureTitle", "Error Reporting")]
        [Xunit.TraitAttribute("Description", "Running out of sessions should give exception")]
        [Xunit.TraitAttribute("Category", "fixed_session_pool")]
        public virtual void RunningOutOfSessionsShouldGiveException()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Running out of sessions should give exception", new string[] {
                        "fixed_session_pool",
                        "ignore"});
#line 40
  this.ScenarioSetup(scenarioInfo);
#line 41
    testRunner.Given("I have a driver with fixed pool size of 1", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 42
    testRunner.And("I store a session", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line 43
    testRunner.When("I try to get a session", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table6 = new TechTalk.SpecFlow.Table(new string[] {
                        "start of message"});
            table6.AddRow(new string[] {
                        "Failed to acquire a session with Neo4j as all the connections in the connection p" +
                            "ool are already occupied by other sessions."});
#line 44
    testRunner.Then("it throws a `ClientException`", ((string)(null)), table6, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Xunit.FactAttribute(Skip="Ignored")]
        [Xunit.TraitAttribute("FeatureTitle", "Error Reporting")]
        [Xunit.TraitAttribute("Description", "Reusing session then running out of sessions should give exception")]
        [Xunit.TraitAttribute("Category", "fixed_session_pool")]
        public virtual void ReusingSessionThenRunningOutOfSessionsShouldGiveException()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Reusing session then running out of sessions should give exception", new string[] {
                        "fixed_session_pool",
                        "ignore"});
#line 49
  this.ScenarioSetup(scenarioInfo);
#line 50
    testRunner.Given("I have a driver with fixed pool size of 1", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 51
    testRunner.When("I start a `Transaction` through a session", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line 52
    testRunner.And("I close the session", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line 53
    testRunner.And("I store a session", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line 54
    testRunner.Then("I get no exception", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Then ");
#line 55
    testRunner.And("I try to get a session", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
            TechTalk.SpecFlow.Table table7 = new TechTalk.SpecFlow.Table(new string[] {
                        "start of message"});
            table7.AddRow(new string[] {
                        "Failed to acquire a session with Neo4j as all the connections in the connection p" +
                            "ool are already occupied by other sessions."});
#line 56
    testRunner.And("it throws a `ClientException`", ((string)(null)), table7, "And ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [System.CodeDom.Compiler.GeneratedCodeAttribute("TechTalk.SpecFlow", "2.0.0.0")]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
        public class FixtureData : System.IDisposable
        {
            
            public FixtureData()
            {
                ErrorReportingFeature.FeatureSetup();
            }
            
            void System.IDisposable.Dispose()
            {
                ErrorReportingFeature.FeatureTearDown();
            }
        }
    }
}
#pragma warning restore
#endregion
