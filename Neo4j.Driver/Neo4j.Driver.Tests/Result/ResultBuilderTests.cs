// Copyright (c) 2002-2018 Neo4j Sweden AB [http://neo4j.com]
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
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.Internal.Result;
using Xunit;

namespace Neo4j.Driver.Tests
{
    public class ResultBuilderTests
    {
        private static ResultBuilder GenerateBuilder(IDictionary<string, object> meta = null)
        {
            var builder = new ResultBuilder(null, () => { }, null, null);
            builder.CollectFields(meta ?? new Dictionary<string, object> { { "fields", new List<object> { "x" } } });
            return builder;
        }

        private static Task AssertGetExpectResults(StatementResult result, int numberExpected, List<object> exspectedRecordsValues = null)
        {
            int count = 0;
            var t = Task.Factory.StartNew(() =>
            {
                // ReSharper disable once LoopCanBeConvertedToQuery
                foreach (var item in result)
                {
                    if (exspectedRecordsValues != null)
                    {
                        item.Values.First().Value.Should().Be(exspectedRecordsValues[count]);
                    }
                    count++;
                }
                count.Should().Be(numberExpected);
            });
            return t;
        }

        public class CollectRecordMethod
        {
            [Fact]
            public void ShouldStreamResults()
            {
                var builder = GenerateBuilder();
                var i = 0;
                builder.SetReceiveOneAction(() =>
                {
                    if (i++ >= 3)
                    {
                        builder.CollectSummary(null);
                    }
                    else
                    {
                        builder.CollectRecord(new object[] {123 + i});
                    }
                });
                var result = builder.PreBuild();

                var t = AssertGetExpectResults(result, 3, new List<object> {124, 125, 126});
                t.Wait();
            }

            [Fact]
            public void ShouldReturnNoResultsWhenNoneRecieved()
            {
                var builder = GenerateBuilder();
                builder.SetReceiveOneAction(() =>
                {
                    builder.CollectSummary(null);
                });
                var result = builder.PreBuild();

                var t = AssertGetExpectResults(result, 0);

                t.Wait();
            }

            [Fact]
            public void ShouldReturnQueuedResultsWithExspectedValue()
            {
                var builder = GenerateBuilder();
                List<object> recordValues = new List<object>
                {
                    1,
                    "Hello",
                    false,
                    10
                };
                for (int i = 0; i < recordValues.Count; i++)
                {
                    builder.CollectRecord(new[] { recordValues[i] });
                }
                builder.CollectSummary(null);

                var result = builder.PreBuild();

                var task = AssertGetExpectResults(result, recordValues.Count, recordValues);
                task.Wait();
            }

            [Fact]
            public void ShouldStopStreamingWhenResultIsInvalid()
            {
                var builder = GenerateBuilder();
                var i = 0;
                builder.SetReceiveOneAction(() =>
                {
                    if (i++ >= 3)
                    {
                        builder.DoneFailure();
                    }
                    else
                    {
                        builder.CollectRecord(new object[] { 123 + i });
                    }
                });
                var result = builder.PreBuild();

                var t = AssertGetExpectResults(result, 3, new List<object> { 124, 125, 126 });
                t.Wait();
            }
        }

        public class CollectFieldsMethod
        {
            [Fact]
            public void ShouldPassDefaultKeysToResultIfNoKeySet()
            {
                var builder = new ResultBuilder(null, () => { }, null, null);
                builder.DoneSuccess();

                var result = builder.PreBuild();

                result.Keys.Should().BeEmpty();
            }

            [Fact]
            public void ShouldDoNothingWhenMetaIsNull()
            {
                var builder = new ResultBuilder(null, () => { }, null, null);
                builder.CollectFields(null);
                builder.DoneSuccess();

                var result = builder.PreBuild();
                result.Keys.Should().BeEmpty();
            }

            [Fact]
            public void ShouldDoNothingWhenMetaDoesNotContainFields()
            {
                var builder = new ResultBuilder(null, () => { }, null, null);
                var meta = new Dictionary<string, object>
                {
                    {"something", "here" }
                };
                builder.CollectFields(meta);
                builder.DoneSuccess();

                var result = builder.PreBuild();
                result.Keys.Should().BeEmpty();
            }

            [Fact]
            public void ShouldCollectKeys()
            {
                IDictionary<string, object> meta = new Dictionary<string, object>
                { {"fields", new List<object> {"fieldKey1", "fieldKey2", "fieldKey3"} },{"type", "r" } };

                var builder = new ResultBuilder(null, () => { }, null, null);
                builder.CollectFields(meta);
                builder.DoneSuccess();
                var result = builder.PreBuild();

                result.Keys.Should().ContainInOrder("fieldKey1", "fieldKey2", "fieldKey3");
            }
        }
    }
}
