﻿// Copyright (c) 2002-2017 "Neo Technology,"
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
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.V1;
using Xunit;
using TaskExtensions = Neo4j.Driver.Internal.TaskExtensions; 

namespace Neo4j.Driver.Tests
{
    public class ResultCursorBuilderTests
    {
        private static ResultCursorBuilder GenerateBuilder(IDictionary<string, object> meta = null)
        {
            var builder = new ResultCursorBuilder();
            builder.CollectFields(meta ?? new Dictionary<string, object> { { "fields", new List<object> { "x" } } });
            return builder;
        }

        private static Task AssertGetExpectResults(IStatementResultCursor cursor, int numberExpected, List<object> exspectedRecordsValues = null)
        {
            int count = 0;
            var t = Task.Factory.StartNew(async () =>
            {
                // ReSharper disable once LoopCanBeConvertedToQuery
                while (await cursor.FetchAsync())
                {
                    if (exspectedRecordsValues != null)
                    {
                        cursor.Current.Values.First().Value.Should().Be(exspectedRecordsValues[count]);
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
            public async void ShouldStreamResults()
            {
                var builder = GenerateBuilder();
                var i = 0;
                builder.SetReceiveOneFunc(() =>
                {
                    if (i++ >= 3)
                    {
                        builder.CollectSummary(null);
                        builder.DoneSuccess();
                    }
                    else
                    {
                        builder.CollectRecord(new object[] {123 + i});
                    }

                    return TaskExtensions.GetCompletedTask();
                });
                var result = await builder.PreBuildAsync();

                var t = AssertGetExpectResults(result, 3, new List<object> {124, 125, 126});

                t.Wait();
            }

            [Fact]
            public async void ShouldReturnNoResultsWhenNoneRecieved()
            {
                var builder = GenerateBuilder();
                builder.SetReceiveOneFunc(() =>
                {
                    builder.CollectSummary(null);

                    builder.DoneSuccess();

                    return TaskExtensions.GetCompletedTask();
                });
                var result = await builder.PreBuildAsync();

                var t = AssertGetExpectResults(result, 0);

                t.Wait();
            }

            [Fact]
            public async void ShouldReturnQueuedResultsWithExspectedValue()
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
                builder.DoneSuccess();

                var result = await builder.PreBuildAsync();

                var task = AssertGetExpectResults(result, recordValues.Count, recordValues);
                task.Wait();
            }

            [Fact]
            public async void ShouldStopStreamingWhenResultIsInvalid()
            {
                var builder = GenerateBuilder();
                var i = 0;
                builder.SetReceiveOneFunc(() =>
                {
                    if (i++ >= 3)
                    {
                        builder.DoneFailure();
                    }
                    else
                    {
                        builder.CollectRecord(new object[] { 123 + i });
                    }

                    return TaskExtensions.GetCompletedTask();
                });
                var result = await builder.PreBuildAsync();

                var t = AssertGetExpectResults(result, 3, new List<object> { 124, 125, 126 });
                t.Wait();
            }
        }
    }
}
