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
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Neo4j.Driver.V1;
using TechTalk.SpecFlow;

using Record = Neo4j.Driver.Internal.Result.Record;

namespace Neo4j.Driver.Tck.Tests.TCK
{
    [Binding]
    public class MatchAcceptanceTestSteps: TckStepsBase
    {
        private readonly CypherRecordParser _parser = new CypherRecordParser();

        [Given(@"init: (.*)$")]
        public void GivenInit(string statement)
        {
            using (var session = Driver.Session())
            {
                session.Run(statement);
            }
        }

        [Given(@"using: cineast")]
        public void GivenUsingCineast()
        {
            ScenarioContext.Current.Pending();
        }
        
        [When(@"running: (.*)$")]
        public void WhenRunning(string statement)
        {
            using (var session = Driver.Session())
            {
                var result = session.Run(statement);
                ScenarioContext.Current.Set(result);
            }
        }

        [Given(@"running: (.*)$")]
        public void GivenRunning(string statement)
        {
            WhenRunning(statement);
        }

        [Then(@"running: (.*)$")]
        public void ThenRunning(string statement)
        {
            WhenRunning(statement);
        }

        [When(@"running parametrized: (.*)$")]
        public void WhenRunningParameterized(string statement, Table table)
        {
            table.RowCount.Should().Be(1);
            var dict = table.Rows[0].Keys.ToDictionary<string, string, object>(key => key, key => _parser.Parse(table.Rows[0][key]));

            using (var session = Driver.Session())
            {
                var resultCursor = session.Run(statement, dict);
                ScenarioContext.Current.Set(resultCursor);
            }
        }

        [Given(@"running parametrized: (.*)$")]
        public void GivenRunningParameterized(string statement, Table table)
        {
            WhenRunningParameterized(statement, table);
        }

        [BeforeScenario("@reset_database")]
        public void ResetDatabase()
        {
            using (var session = Driver.Session())
            {
                session.Run("MATCH (n) DETACH DELETE n");
            }
        }

        [Then(@"result:")]
        public void ThenResult(Table table)
        {
            var records = new List<IRecord>();
            foreach (var row in table.Rows)
            {
                records.Add( new Record(row.Keys.ToArray(), row.Values.Select(value => _parser.Parse(value)).ToArray()));
            }
            var resultCursor = ScenarioContext.Current.Get<IStatementResult>();
            AssertRecordsAreTheSame(resultCursor.ToList(), records);
        }
    }
}
