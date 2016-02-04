using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using TechTalk.SpecFlow;
using Xunit;

namespace Neo4j.Driver.IntegrationTests.TCK
{
    public abstract class TckStepsBase
    {
        protected dynamic _expected;
        protected IList<object> _list;
        protected IDictionary<string, object> _map;
        public const string Url = "bolt://localhost:7687";
        protected static Driver Driver;
        protected static Neo4jInstaller _installer;

        protected dynamic GetValue(string type, string value)
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

        
    }

    [Binding]
    public class DriverTypesTestEchoingSingleParameterSteps : TckStepsBase
    {
        private ResultCursor _result;

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
                try { AfterScenario(); } catch { /*Do Nothing*/ }
                throw;
            }
            Driver = GraphDatabase.Driver(Url);
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
                _list.Add((object)GetValue(type, value));
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
                _map.Add($"Key{i++}", (object)GetValue(type, value));
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
            _result = Driver.Session().Run("Return {input}", new Dictionary<string, object> {{"input", _expected}});
        }

        [When(@"the driver asks the server to echo this list back")]
        public void WhenTheDriverAsksTheServerToEchoThisListBack()
        {
            _expected = _list;
            _result = Driver.Session().Run("Return {input}", new Dictionary<string, object> { { "input", _expected } });
        }

        [When(@"the driver asks the server to echo this map back")]
        public void WhenTheDriverAsksTheServerToEchoThisMapBack()
        {
            _expected = _map;
            _result = Driver.Session().Run("Return {input}", new Dictionary<string, object> { { "input", _expected } });
        }

        [When(@"the value given in the result should be the same as what was sent")]
        public void WhenTheValueGivenInTheResultShouldBeTheSameAsWhatWasSent()
        {
            // param : input
            _result.Single().Should().BeTrue();
            var actual = _result.Get(0);
            AssertEqual(_expected, actual);
        }

        [When(@"adding a table of lists to the map M")]
        public void WhenAddingATableOfListsToTheMapM(Table table)
        {
            int i = 0;
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
            int i = 0;
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

        private void AssertEqual(dynamic value, dynamic other)
        {
            if (value == null || value is bool || value is long || value is double || value is string)
            {
                Assert.Equal(value, other);
            }
            else if (value is IList)
            {
                var valueList = (IList) value;
                var otherList = (IList) other;
                AssertEqual(valueList.Count, otherList.Count);
                for (var i = 0; i < valueList.Count; i++)
                {
                    AssertEqual(valueList[i], otherList[i]);
                }
            }
            else if (value is IDictionary)
            {
                var valueDic = (IDictionary<string, object>) value;
                var otherDic = (IDictionary<string, object>) other;
                AssertEqual(valueDic.Count, otherDic.Count);
                foreach (var key in valueDic.Keys)
                {
                    otherDic.ContainsKey(key).Should().BeTrue();
                    AssertEqual(valueDic[key], otherDic[key]);
                }
            }
        }
    }
}