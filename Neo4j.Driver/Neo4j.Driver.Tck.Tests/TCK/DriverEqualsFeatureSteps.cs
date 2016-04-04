using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using TechTalk.SpecFlow;

namespace Neo4j.Driver.Tck.Tests.TCK
{
    [Binding]
    public class DriverEqualsFeatureSteps : TckStepsBase
    {
        private const string ValueListkey = "ValueListOfDriverEquals";
        [Given(@"`(.*)` is single value result of: (.*)")]
        public void GivenValue1IsSingleValueResultOf(string key, string statement)
        {
            using (var session = Driver.Session())
            {
                var statementResult = session.Run(statement);
                var value = statementResult.Single()[0];
                
                if (!ScenarioContext.Current.ContainsKey(ValueListkey))
                {
                    ScenarioContext.Current.Set(new List<object>(), ValueListkey);
                }
                var values = ScenarioContext.Current.Get<IList<object>>(ValueListkey);

                values.Add(value);
            }
        }

        [When(@"`(.*)` is single value result of: (.*)")]
        public void WhenValue1IsSingleValueResultOf(string key, string statement)
        {
            GivenValue1IsSingleValueResultOf(key, statement);
        }

        [Then(@"saved values should all equal")]
        public void ThenSavedValuesShouldAllEqual()
        {
            var values = ScenarioContext.Current.Get<IList<object>>(ValueListkey);
            values.Count.Should().BeGreaterOrEqualTo(2);
            var first = values.First();
            foreach (var value in values)
            {
                first.Equals(value).Should().BeTrue();
                value.Equals(first).Should().BeTrue();
            }
        }

        [Then(@"none of the saved values should be equal")]
        public void ThenNoneOfTheSavedValuesShouldBeEqual()
        {
            var values = ScenarioContext.Current.Get<IList<object>>(ValueListkey);
            values.Count.Should().BeGreaterOrEqualTo(2);
            for (int i = 0; i < values.Count; i++)
            {
                var first = values[i];
                for (int j = i+1; j < values.Count; j++)
                {
                    var second = values[j];
                    first.Equals(second).Should().BeFalse();
                    second.Equals(first).Should().BeFalse();
                }
            }
        }
    }
}
