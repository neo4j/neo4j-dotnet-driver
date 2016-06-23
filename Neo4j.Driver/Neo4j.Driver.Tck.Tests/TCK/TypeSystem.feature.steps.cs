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
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using Neo4j.Driver.V1;
using TechTalk.SpecFlow;

namespace Neo4j.Driver.Tck.Tests.TCK
{
    [Binding]
    public class DriverTypesTestEchoingSingleParameterSteps
    {
        private IStatementResult _statementResult;
        public const string KeyExpected = "expected";
        public const string KeyList = "list";
        public const string Keymap = "map";

        [Given(@"A running database")]
        public void GivenARunningDatabase()
        {
        }

        [Given(@"a value (.*) of type (.*)")]
        public void GivenAValueOfType(string value, string type)
        {
            ScenarioContext.Current.Set(TckUtil.GetValue(type, value), KeyExpected);
        }

        [Given(@"a list value (.*) of type (.*)")]
        public void GivenAListValueOfTypeInteger(string values, string type)
        {
            ScenarioContext.Current.Set(GetList(type, values), KeyExpected);
        }

        [Given(@"an empty list L")]
        public void GivenAnEmptyListL()
        {
            ScenarioContext.Current.Set(new List<object>(), KeyList);
        }

        [Given(@"adding a table of lists to the list L")]
        public void GivenAddingATableOfListsToTheListL(Table table)
        {
            var list = ScenarioContext.Current.Get<IList<object>>(KeyList);
            foreach (var row in table.Rows)
            {
                var columns = row.Values;
                var type = columns.ElementAt(0);
                var values = columns.ElementAt(1);
                list.Add(GetList(type, values));
            }
        }

        [Given(@"adding a table of values to the list L")]
        public void GivenAddingATableOfValuesToTheListL(Table table)
        {
            var list = ScenarioContext.Current.Get<IList<object>>(KeyList);
            foreach (var row in table.Rows)
            {
                var columns = row.Values;
                var type = columns.ElementAt(0);
                var value = columns.ElementAt(1);
                list.Add(TckUtil.GetValue(type, value));
            }
        }

        [Given(@"an empty map M")]
        public void GivenAnEmptyMapM()
        {
            ScenarioContext.Current.Set(new Dictionary<string, object>(), Keymap);
        }

        [Given(@"adding a table of values to the map M")]
        public void GivenAddingATableOfValuesToTheMapM(Table table)
        {
            var i = 0;
            var map = ScenarioContext.Current.Get<IDictionary<string, object>>(Keymap);
            foreach (var row in table.Rows)
            {
                var columns = row.Values;
                var type = columns.ElementAt(0);
                var value = columns.ElementAt(1);
                map.Add($"Key{i++}", TckUtil.GetValue(type, value));
            }
        }

        [Given(@"adding map M to list L")]
        public void GivenAddingMapMToListL()
        {
            var list = ScenarioContext.Current.Get<IList<object>>(KeyList);
            var map = ScenarioContext.Current.Get<IDictionary<string, object>>(Keymap);
            list.Add(map);
        }

        [When(@"the driver asks the server to echo this value back")]
        public void WhenTheDriverAsksTheServerToEchoThisValueBack()
        {
            var expected = ScenarioContext.Current.Get<object>(KeyExpected);
            using (var session = TckHooks.Driver.Session())
            {
                _statementResult = session.Run("Return {input}", new Dictionary<string, object> { { "input", expected } });
            }
            
        }

        [When(@"the driver asks the server to echo this list back")]
        public void WhenTheDriverAsksTheServerToEchoThisListBack()
        {
            var list = ScenarioContext.Current.Get<IList<object>>(KeyList);
            ScenarioContext.Current.Set((object)list, KeyExpected);
            using (var session = TckHooks.Driver.Session())
            {
                _statementResult = session.Run("Return {input}", new Dictionary<string, object> { { "input", list } });
            }
            
        }

        [When(@"the driver asks the server to echo this map back")]
        public void WhenTheDriverAsksTheServerToEchoThisMapBack()
        {
            var map = ScenarioContext.Current.Get<IDictionary<string, object>>(Keymap);
            ScenarioContext.Current.Set((object)map, KeyExpected);
            using (var session = TckHooks.Driver.Session())
            {
                _statementResult = session.Run("Return {input}", new Dictionary<string, object> { { "input", map } });
            }
        }

        [When(@"the value given in the result should be the same as what was sent")]
        public void WhenTheValueGivenInTheResultShouldBeTheSameAsWhatWasSent()
        {
            // param : input
            var record = _statementResult.Single();
            record.Should().NotBeNull();
            var actual = record[0];
            var expected = ScenarioContext.Current.Get<object>(KeyExpected);
            TckUtil.AssertEqual(expected, actual);
        }

        [When(@"adding a table of lists to the map M")]
        public void WhenAddingATableOfListsToTheMapM(Table table)
        {
            var i = 0;
            var map = ScenarioContext.Current.Get<IDictionary<string, object>>(Keymap);
            foreach (var row in table.Rows)
            {
                var columns = row.Values;
                var type = columns.ElementAt(0);
                var values = columns.ElementAt(1);
                map.Add($"ListKey{i++}", GetList(type, values));
            }
        }

        [When(@"adding a table of values to the map M")]
        public void WhenAddingATableOfValuesToTheMapM(Table table)
        {
            var i = 0;
            var map = ScenarioContext.Current.Get<IDictionary<string, object>>(Keymap);
            foreach (var row in table.Rows)
            {
                var columns = row.Values;
                var type = columns.ElementAt(0);
                var value = columns.ElementAt(1);
                map.Add($"Key{i++}", TckUtil.GetValue(type, value));
            }
        }

        [When(@"adding a copy of map M to map M")]
        public void WhenAddingACopyOfMapMToMapM()
        {
            var map = ScenarioContext.Current.Get<IDictionary<string, object>>(Keymap);
            map.Add("MapKey", new Dictionary<string, object>(map));
        }


        private List<object> GetList(string type, string values)
        {
            var strings = values.Split(new[] {",", "[", "]"}, StringSplitOptions.RemoveEmptyEntries);
            return strings.Select(value => TckUtil.GetValue(type, value)).ToList();
        }
        
    }
}