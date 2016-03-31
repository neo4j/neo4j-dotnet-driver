using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using FluentAssertions;
using TechTalk.SpecFlow;
using Xunit;

namespace Neo4j.Driver.Tck.Tests
{
    [Binding]
    public class DriverResultApiSteps
    {
        
        [When(@"the `Statement Result` is consumed a `Result Summary` is returned")]
        public void WhenTheStatementResultIsConsumedAResultSummaryIsReturned()
        {
            var result = ScenarioContext.Current.Get<IStatementResult>();
            var resultSummary = result.Consume();
            ScenarioContext.Current.Set(resultSummary);
        }

        [When(@"I request a `Statement` from the `Result Summary`")]
        public void WhenIRequestAStatementFromTheResultSummary()
        {
            var summary = ScenarioContext.Current.Get<IResultSummary>();
            ScenarioContext.Current.Set(summary);
        }

        [Then(@"the `Statement Result` is closed")]
        public void ThenTheStatementResultIsClosed()
        {
            var result = ScenarioContext.Current.Get<IStatementResult>();
            result.Peek().Should().BeNull();
            result.GetEnumerator().MoveNext().Should().BeFalse();
            result.GetEnumerator().Current.Should().BeNull();
        }
        
        [Then(@"requesting the `Statement` as text should give: (.*)")]
        public void ThenRequestingTheStatementAsTextShouldGive(string statement)
        {
            var summary = ScenarioContext.Current.Get<IResultSummary>();
            summary.Statement.Template.Should().Be(statement);
        }

        [Then(@"requesting the `Statement` parameter should give: \{}")]
        public void ThenRequestingTheStatementParameterShouldGiveEmptyParam()
        {
            var summary = ScenarioContext.Current.Get<IResultSummary>();
            summary.Statement.Parameters.Count.Should().Be(0);
            summary.Statement.Parameters.Should().BeEmpty();
        }
        
        
        [Then(@"requesting the `Statement` parameter should give: \{""param"":""Pelle""\}")]
        public void ThenRequestingTheStatementParameterShouldGive()
        {
            var summary = ScenarioContext.Current.Get<IResultSummary>();
            var parameters = summary.Statement.Parameters;
            parameters.Count.Should().Be(1);
            parameters.Keys.Count.Should().Be(1);
            parameters.Keys.Should().Contain("param");

            parameters["param"].As<string>().Should().Be("Pelle");
        }
        
        [Then(@"requesting `Counters` from `Result Summary` should give")]
        public void ThenRequestingCountersFromResultSummaryShouldGive(Table table)
        {
            var summary = ScenarioContext.Current.Get<IResultSummary>();
            var counters = summary.Counters;
            foreach (var row in table.Rows)
            {
                var objType = counters.GetType();
                TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
                
                var propName = textInfo.ToTitleCase(row["counter"]).Replace(" ", string.Empty);

                var actual = objType.GetProperty(propName).GetValue(counters);
                actual.ToString().ToLower().Should().Be(row["result"]);
            }
        }
        
        [Then(@"requesting the `Statement Type` should give (.*)")]
        public void ThenRequestingTheStatementTypeShouldGive(string expected)
        {
            var summary = ScenarioContext.Current.Get<IResultSummary>();
            var actual = summary.StatementType;
            if (expected == "read only")
            {
                actual.Should().Be(StatementType.ReadOnly);
            }
            else if (expected == "read write")
            {
                actual.Should().Be(StatementType.ReadWrite);
            }
            else if (expected == "write only")
            {
                actual.Should().Be(StatementType.WriteOnly);
            }
            else if (expected == "schema write")
            {
                actual.Should().Be(StatementType.SchemaWrite);
            }
            else
            {
                throw new InvalidOperationException($"Cannot understand the type {expected} in the feature file, while the acutal type returned from the server is {actual}"); 
            }
        }

        [Then(@"the `Result Summary` has a `Plan`")]
        public void ThenTheResultSummaryHasAPlan()
        {
            var summary = ScenarioContext.Current.Get<IResultSummary>();
            summary.HasPlan.Should().BeTrue();
        }

        [Then(@"the `Result Summary` does not have a `Profile`")]
        public void ThenTheResultSummaryDoesNotHaveAProfile()
        {
            var summary = ScenarioContext.Current.Get<IResultSummary>();
            summary.HasProfile.Should().BeFalse();
            summary.Profile.Should().BeNull();
        }
        
        [Then(@"requesting the `(.*)` it contains")]
        public void ThenRequestingThePlanItContains(string type, Table table)
        {
            object plan = ParsePlanOrProfile(type);
            foreach (var row in table.Rows)
            {
                var actual = GetPropertyValue(plan, row[0]);
                row[1].Should().Be(actual.ToString());
            }
        }

        [Then(@"the `(.*)` also contains method calls for:")]
        public void ThenThePlanAlsoContainsMethodCallsFor(string type, Table table)
        {
            object plan = ParsePlanOrProfile(type);

            foreach (var row in table.Rows)
            {
                var actual = GetPropertyValue(plan, row[0]);

                var expectedValue = row[1];
                if (expectedValue == "[\"n\"]")
                {
                    actual.Should().BeOfType<List<string>>();
                    IList<string> value = (IList<string>) actual;
                    value.Count.Should().Be(1);
                    value[0].Should().Be("n");
                }
                else if (expectedValue == "list of plans")
                {
                    actual.Should().BeOfType<List<IPlan>>();
                }
                else if (expectedValue == "map of string, values")
                {
                    actual.Should().BeOfType<Dictionary<string, object>>();
                }
                else if (expectedValue == "list of profiled plans")
                {
                    actual.Should().BeOfType<List<IProfiledPlan>>();
                }
                else
                {
                    throw new InvalidOperationException($"Cannot understand plan method type {expectedValue}");
                }
            }
        }

        [Then(@"the `Result Summary` does not have a `Plan`")]
        public void ThenTheResultSummaryDoesNotHaveAPlan()
        {
            var summary = ScenarioContext.Current.Get<IResultSummary>();
            summary.HasPlan.Should().BeFalse();
            summary.Plan.Should().BeNull();
        }

        [Then(@"the `Result Summary` has a `Profile`")]
        public void ThenTheResultSummaryHasAProfile()
        {
            var summary = ScenarioContext.Current.Get<IResultSummary>();
            summary.HasProfile.Should().BeTrue();
        }

        private object GetPropertyValue(object obj, string key)
        {
            var objType = obj.GetType();
            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;

            var propName = textInfo.ToTitleCase(key).Replace(" ", string.Empty);

            return objType.GetProperty(propName).GetValue(obj);
        }

        private object ParsePlanOrProfile(string type)
        {
            var summary = ScenarioContext.Current.Get<IResultSummary>();
            if (type == "Plan")
            {
                return summary.Plan;
            }
            if (type == "Profile")
            {
                return summary.Profile;
            }
            throw new InvalidOperationException($"Cannot understand type {type} used in the feature file.");
        }

        [Then(@"the `Result Summary` `Notifications` is empty")]
        public void ThenTheResultSummaryNotificationsIsEmpty()
        {
            var summary = ScenarioContext.Current.Get<IResultSummary>();
            summary.Notifications.Should().BeEmpty();
        }

        [Then(@"the `Result Summary` `Notifications` has one notification with")]
        public void ThenTheResultSummaryNotificationsHasOneNotificationWith(Table table)
        {
            var summary = ScenarioContext.Current.Get<IResultSummary>();
            var notifications = summary.Notifications;
            foreach (var row in table.Rows)
            {
                var actual = GetPropertyValue(notifications, row[0]);
                row[1].Should().Be(actual.ToString());
            }
        }
    }
}
