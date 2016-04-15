// Copyright (c) 2002-2016 "Neo Technology,"
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
