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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Neo4j.Driver.IntegrationTests;
using TechTalk.SpecFlow;
using Xunit;

namespace Neo4j.Driver.Tck.Tests.TCK
{
    public abstract class TckStepsBase
    {
        public const string Url = "bolt://localhost:7687";
        protected static Driver Driver;
        protected static Neo4jInstaller _installer;
        protected dynamic _expected;
        protected IList<object> _list;
        protected IDictionary<string, object> _map;

        protected static dynamic GetValue(string type, string value)
        {
            switch (type)
            {
                case "Null":
                    return null;
                case "Boolean":
                    return Convert.ToBoolean(value);
                case "Integer":
                    return Convert.ToInt64(value);
                case "Float":
                    return Convert.ToDouble(value);
                case "String":
                    return value;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, $"Unknown type {type}");
            }
        }
        protected static void AssertEqual(dynamic value, dynamic other)
        {
            if (value == null || value is bool || value is long || value is double || value is string)
            {
                Assert.Equal(value, other);
            }
            else if (value is IList)
            {
                var valueList = (IList)value;
                var otherList = (IList)other;
                AssertEqual(valueList.Count, otherList.Count);
                for (var i = 0; i < valueList.Count; i++)
                {
                    AssertEqual(valueList[i], otherList[i]);
                }
            }
            else if (value is IDictionary)
            {
                var valueDic = (IDictionary<string, object>)value;
                var otherDic = (IDictionary<string, object>)other;
                AssertEqual(valueDic.Count, otherDic.Count);
                foreach (var key in valueDic.Keys)
                {
                    otherDic.ContainsKey(key).Should().BeTrue();
                    AssertEqual(valueDic[key], otherDic[key]);
                }
            }
        }
    }

    [Binding]
    public class DriverTypesTestEchoingSingleParameterSteps : TckStepsBase
    {
        private IStatementResult _statementResult;

        [BeforeTestRun]
        public static void GlobalBeforeScenario()
        {
            _installer = new Neo4jInstaller();
            _installer.DownloadNeo4j().Wait();
            try
            {
                _installer.InstallServer();
                _installer.StartServer();
            }
            catch
            {
                try
                {
                    AfterScenario();
                }
                catch
                {
                    /*Do Nothing*/
                }
                throw;
            }
            var config = Config.DefaultConfig;
            config.IdleSessionPoolSize = Config.InfiniteIdleSessionPoolSize;
            Driver = GraphDatabase.Driver(Url, config);
        }

        [AfterTestRun]
        public static void AfterScenario()
        {
            Driver?.Dispose();

            try
            {
                _installer.StopServer();
            }
            catch
            {
                // ignored
            }
            _installer.UninstallServer();
        }

        [Given(@"A running database")]
        public void GivenARunningDatabase()
        {
        }

        [Given(@"a value (.*) of type (.*)")]
        public void GivenAValueOfType(string value, string type)
        {
            _expected = GetValue(type, value);
        }

        [Given(@"a list value (.*) of type (.*)")]
        public void GivenAListValueOfTypeInteger(string values, string type)
        {
            _expected = GetList(type, values);
        }

        [Given(@"an empty list L")]
        public void GivenAnEmptyListL()
        {
            _list = new List<object>();
        }

        [Given(@"adding a table of lists to the list L")]
        public void GivenAddingATableOfListsToTheListL(Table table)
        {
            foreach (var row in table.Rows)
            {
                var columns = row.Values;
                var type = columns.ElementAt(0);
                var values = columns.ElementAt(1);
                _list.Add(GetList(type, values));
            }
        }

        [Given(@"adding a table of values to the list L")]
        public void GivenAddingATableOfValuesToTheListL(Table table)
        {
            foreach (var row in table.Rows)
            {
                var columns = row.Values;
                var type = columns.ElementAt(0);
                var value = columns.ElementAt(1);
                _list.Add((object) GetValue(type, value));
            }
        }

        [Given(@"an empty map M")]
        public void GivenAnEmptyMapM()
        {
            _map = new Dictionary<string, object>();
        }

        [Given(@"adding a table of values to the map M")]
        public void GivenAddingATableOfValuesToTheMapM(Table table)
        {
            var i = 0;
            foreach (var row in table.Rows)
            {
                var columns = row.Values;
                var type = columns.ElementAt(0);
                var value = columns.ElementAt(1);
                _map.Add($"Key{i++}", (object) GetValue(type, value));
            }
        }


        [Given(@"adding map M to list L")]
        public void GivenAddingMapMToListL()
        {
            _list.Add(_map);
        }

        [When(@"the driver asks the server to echo this value back")]
        public void WhenTheDriverAsksTheServerToEchoThisValueBack()
        {
            _statementResult = Driver.Session().Run("Return {input}", new Dictionary<string, object> {{"input", _expected}});
        }

        [When(@"the driver asks the server to echo this list back")]
        public void WhenTheDriverAsksTheServerToEchoThisListBack()
        {
            _expected = _list;
            _statementResult = Driver.Session().Run("Return {input}", new Dictionary<string, object> {{"input", _expected}});
        }

        [When(@"the driver asks the server to echo this map back")]
        public void WhenTheDriverAsksTheServerToEchoThisMapBack()
        {
            _expected = _map;
            _statementResult = Driver.Session().Run("Return {input}", new Dictionary<string, object> {{"input", _expected}});
        }

        [When(@"the value given in the result should be the same as what was sent")]
        public void WhenTheValueGivenInTheResultShouldBeTheSameAsWhatWasSent()
        {
            // param : input
            var record = _statementResult.Single(); // TODO check no exception throw
            record.Should().NotBeNull();
            var actual = record[0];
            AssertEqual(_expected, actual);
        }

        [When(@"adding a table of lists to the map M")]
        public void WhenAddingATableOfListsToTheMapM(Table table)
        {
            var i = 0;
            foreach (var row in table.Rows)
            {
                var columns = row.Values;
                var type = columns.ElementAt(0);
                var values = columns.ElementAt(1);
                _map.Add($"ListKey{i++}", GetList(type, values));
            }
        }

        [When(@"adding a table of values to the map M")]
        public void WhenAddingATableOfValuesToTheMapM(Table table)
        {
            var i = 0;
            foreach (var row in table.Rows)
            {
                var columns = row.Values;
                var type = columns.ElementAt(0);
                var value = columns.ElementAt(1);
                _map.Add($"Key{i++}", GetValue(type, value));
            }
        }

        [When(@"adding a copy of map M to map M")]
        public void WhenAddingACopyOfMapMToMapM()
        {
            _map.Add("MapKey", new Dictionary<string, object>(_map));
        }


        private List<object> GetList(string type, string values)
        {
            var strings = values.Split(new[] {",", "[", "]"}, StringSplitOptions.RemoveEmptyEntries);
            return strings.Select(value => (object) GetValue(type, value)).ToList();
        }
        
    }
}