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
using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Neo4j.Driver.V1;
using TechTalk.SpecFlow;
using Xunit;
using Record = Neo4j.Driver.Internal.Result.Record;

namespace Neo4j.Driver.Tck.Tests.TCK
{
    [Binding]
    public class DriverStatementResultNavigationSteps
    {
        private readonly CypherRecordParser _parser = new CypherRecordParser();

        [When(@"using `Next` on `Statement Result` gives a `Record`")]
        public void WhenUsingNextOnStatementResultGivesARecord()
        {
            var result = ScenarioContext.Current.Get<IStatementResult>();
            var record = result.First();
            record.Should().NotBeNull();
        }
        
        [Then(@"using `Single` on `Statement Result` gives a `Record` containing:")]
        public void ThenUsingSingleOnStatementResultGivesARecordContaining(Table table)
        {
            table.RowCount.Should().Be(1);
            var expected = Convert.ToInt32(table.Rows[0][0]);
            var result = ScenarioContext.Current.Get<IStatementResult>();
            var record = result.Single();
            record.Keys.Count.Should().Be(1);
            record[0].ValueAs<int>().Should().Be(expected);
        }

        [Then(@"using `Single` on `Statement Result` throws exception:")]
        public void ThenUsingSingleOnStatementResultThrowsException(Table table)
        {
            var result = ScenarioContext.Current.Get<IStatementResult>();
            var ex = Xunit.Record.Exception(()=>result.Single());
            ex.Should().BeOfType<InvalidOperationException>();
        }
        
        [Then(@"iterating through the `Statement Result` should follow the native code pattern")]
        public void ThenIteratingThroughTheStatementResultShouldFollowTheNativeCodePattern()
        {
            var result = ScenarioContext.Current.Get<IStatementResult>();
            foreach (var record in result)
            {
                foreach (var value in record.Values)
                {
                    value.Value.ToString();
                }
            }
        }

        [Then(@"using `Peek` on `Statement Result` gives a `Record` containing:")]
        public void ThenUsingPeekOnStatementResultGivesARecordContaining(Table table)
        {
            table.RowCount.Should().Be(1);
            var expected = Convert.ToInt32(table.Rows[0][0]);
            var result = ScenarioContext.Current.Get<IStatementResult>();
            var record = result.Peek();
            record.Keys.Count.Should().BeGreaterOrEqualTo(1);
            record[0].ValueAs<int>().Should().Be(expected);
        }
        
        [Then(@"using `Next` on `Statement Result` gives a `Record` containing:")]
        public void ThenUsingNextOnStatementResultGivesARecordContaining(Table table)
        {
            table.RowCount.Should().Be(1);
            var expected = Convert.ToInt32(table.Rows[0][0]);
            var result = ScenarioContext.Current.Get<IStatementResult>();
            var record = result.First();
            record.Keys.Count.Should().Be(1);
            record[0].ValueAs<int>().Should().Be(expected);
        }
        
        [Then(@"using `Peek` on `Statement Result` gives null")]
        public void ThenUsingPeekOnStatementResultGivesNull()
        {
            var result = ScenarioContext.Current.Get<IStatementResult>();
            result.Peek().Should().BeNull();
        }
        
        [Then(@"using `Next` on `Statement Result` gives null")]
        public void ThenUsingNextOnStatementResultGivesNull()
        {
            var result = ScenarioContext.Current.Get<IStatementResult>();
            var ex = Xunit.Record.Exception(() => result.First());
            ex.Should().BeOfType<InvalidOperationException>();
        }

        [Then(@"it is not possible to go back")]
        public void ThenItIsNotPossibleToGoBack()
        {
        }

        [Then(@"using `Keys` on `Statement Result` gives:")]
        public void ThenUsingKeysOnStatementResultGives(Table table)
        {
            var result = ScenarioContext.Current.Get<IStatementResult>();
            var list = table.Rows.Select(row => row[0]).ToList();
            result.Consume();
            list.Count.Should().Be(result.Keys.Count);
            foreach (var key in result.Keys)
            {
                list.Contains(key.ValueAs<string>()).Should().BeTrue();
            }
        }
        
        [Then(@"using `List` on `Statement Result` gives:")]
        public void ThenUsingListOnStatementResultGives(Table table)
        {
            var records = new List<IRecord>();
            foreach (var row in table.Rows)
            {
                records.Add(new Record(row.Keys.ToList(), row.Values.Select(value => _parser.Parse(value)).ToArray()));
            }
            var resultCursor = ScenarioContext.Current.Get<IStatementResult>();
            TckUtil.AssertRecordsAreTheSame(resultCursor.ToList(), records);
        }

        [Then(@"using `List` on `Statement Result` gives a list of size (.*), the previous records are lost")]
        public void ThenUsingListOnStatementResultGivesAListOfSizeThePreviousRecordsAreLost(int size)
        {
            var result = ScenarioContext.Current.Get<IStatementResult>();
            var records = result.ToList();
            records.Count.Should().Be(size);
        }

        [Then(@"using `Keys` on the single record gives:")]
        public void ThenUsingKeysOnTheSingleRecordGives(Table table)
        {
            var result = ScenarioContext.Current.Get<IStatementResult>();
            var keys = result.Single().Keys;
            keys.Count.Should().Be(table.RowCount);
            foreach (var row in table.Rows)
            {
                row.Values.Count.Should().Be(1);
                keys.Contains(row[0]).Should().BeTrue();
            }
        }

        [Then(@"using `Values` on the single record gives:")]
        public void ThenUsingValuesOnTheSingleRecordGives(Table table)
        {
            var result = ScenarioContext.Current.Get<IStatementResult>();
            var values = result.Single().Values;
            var expectedValues = table.Rows.Select(row => _parser.Parse(row[0])).Select(expected => expected).Cast<object>().ToList();
            while (expectedValues.Count > 0)
            {
                var expected = expectedValues.First();
                var size = expectedValues.Count;
                foreach (var real in values.Values)
                {
                    if (TckUtil.CypherValueEquals(expected, real))
                    {
                        expectedValues.Remove(expected);
                        break;
                    }
                }
                if (size == expectedValues.Count)
                {
                    throw new Exception($"Failed to found {expected}");
                }
            }

        }

        [Then(@"using `Get` with index (.*) on the single record gives:")]
        public void ThenUsingGetWithIndexOnTheSingleRecordGives(int index, Table table)
        {
            var result = ScenarioContext.Current.Get<IStatementResult>();
            var record = result.Single();
            var actual = record[index];
            var expected = _parser.Parse(table.Rows[0][0]);
            Assert.True(TckUtil.CypherValueEquals(actual, expected));
        }

        [Then(@"using `Get` with key `(.*)` on the single record gives:")]
        public void ThenUsingGetWithKeyNOnTheSingleRecordGives(string key, Table table)
        {
            var result = ScenarioContext.Current.Get<IStatementResult>();
            var record = result.Single();
            var actual = record[key];
            var expected = _parser.Parse(table.Rows[0][0]);
            Assert.True(TckUtil.CypherValueEquals(actual, expected));
        }
    }
}
