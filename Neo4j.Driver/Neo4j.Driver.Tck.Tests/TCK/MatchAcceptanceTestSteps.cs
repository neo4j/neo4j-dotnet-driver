using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using TechTalk.SpecFlow;
using static Neo4j.Driver.Tck.Tests.TCK.CypherRecordParser;
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

        private void AssertRecordsAreTheSame(List<IRecord> actual, List<IRecord> expected)
        {
            actual.Should().HaveSameCount(expected);
            foreach (var aRecord in actual)
            {
                AssertContains(expected, aRecord).Should().BeTrue();
            }
        }

        private bool AssertContains(List<IRecord> records, IRecord aRecord)
        {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var record in records)
            {
                if (RecordEquals(record, aRecord))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool RecordEquals(IRecord r1, IRecord r2)
        {
            return r1.Keys.SequenceEqual(r2.Keys) && r1.Keys.All(key => CypherValueEquals(r1[key], r2[key]));
        }

        private static bool CypherValueEquals(object o1, object o2)
        {
            // long/double/bool/null/string/list<object>/dict<string, object>/path/node/rel
            if (ReferenceEquals(o1, o2)) return true;
            if (o1.GetType() != o2.GetType()) return false;
            if (o1 is string)
            {
                return (string) o1 == (string) o2;
            }
            if (o1 is IDictionary<string, object>)
            {
                var dict = (IDictionary<string, object>)o1;
                var dict2 = (IDictionary<string, object>) o2;
                return dict.Keys.SequenceEqual(dict2.Keys) && dict.Keys.All(key => CypherValueEquals(dict[key], dict2[key]));
            }
            if (o1 is IList<object>)
            {
                var list1 = (IList<object>)o1;
                var list2 = (IList<object>)o2;
                if (list1.Count != list2.Count)
                {
                    return false;
                }
                return !list1.Where((t, i) => !CypherValueEquals(t, list2[i])).Any();
            }
            if (o1 is INode)
            {
                return NodeToString((INode) o1) == NodeToString((INode) o2);
            }
            if (o1 is IRelationship)
            {
                return RelToString((IRelationship) o1) == RelToString((IRelationship) o2);
            }
            if (o1 is IPath)
            {
                return PathToString((IPath) o1) == PathToString((IPath) o2);
            }
            return Equals(o1, o2);
        }
    }
}
