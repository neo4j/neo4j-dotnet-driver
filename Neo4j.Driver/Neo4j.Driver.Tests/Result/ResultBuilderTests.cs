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

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.V1;
using Xunit;

namespace Neo4j.Driver.Tests
{
    public class ResultBuilderTests
    {
        public class CollectMetaMethod
        {
            [Fact]
            public void ShouldCollectKeys()
            {
                var builder = new ResultBuilder();
                IDictionary<string, object> meta = new Dictionary<string, object>
                { {"fields", new List<object> {"fieldKey1", "fieldKey2", "fieldKey3"} },{"type", "r" } };
                builder.CollectFields(meta);

                var result = builder.Build();
                result.Keys.Should().ContainInOrder("fieldKey1", "fieldKey2", "fieldKey3");
            }

            [Fact]
            public void ShouldCollectType()
            {
                var builder = new ResultBuilder();
                builder.IsStreamingRecords(false);
                IDictionary<string, object> meta = new Dictionary<string, object>
                { {"type", "r" } };
                builder.CollectSummaryMeta(meta);

                var result = builder.Build();
                result.Consume();
                result.Summary.StatementType.Should().Be(StatementType.ReadOnly);
            }

            [Fact]
            public void ShouldCollectStattistics()
            {
                var builder = new ResultBuilder();
                builder.IsStreamingRecords(false);
                IDictionary<string, object> meta = new Dictionary<string, object>
                { {"type", "r" }, {"stats", new Dictionary<string, object> { {"nodes-created", 10L}, {"nodes-deleted", 5L} } } };
                builder.CollectSummaryMeta(meta);

                var result = builder.Build();
                result.Consume();
                var statistics = result.Summary.Counters;
                statistics.NodesCreated.Should().Be(10);

            }

            [Fact]
            public void ShouldCollectNotifications()
            {
                var builder = new ResultBuilder();
                builder.IsStreamingRecords(false);
                IDictionary<string, object> meta = new Dictionary<string, object>
                {
                    {"type", "r" },
                    {
                        "notifications", new List<object>
                        {
                            new Dictionary<string, object> {{"code", "CODE"}, {"title", "TITLE"}},
                            new Dictionary<string, object>
                            {
                                {"description", "DES"},
                                {
                                    "position", new Dictionary<string, object>
                                    {
                                        {"offset", 11L}
                                    }
                                }
                            }
                        }
                    }
                };

                builder.CollectSummaryMeta(meta);

                InputPosition position = new InputPosition(0,0,0);

                var result = builder.Build();
                result.Consume();
                var notifications = result.Summary.Notifications;
                notifications.Should().HaveCount(2);
                notifications[0].Code.Should().Be("CODE");
                notifications[0].Title.Should().Be("TITLE");
                notifications[0].Position.Offset.Should().Be(0);
                notifications[0].Position.Column.Should().Be(0);
                notifications[0].Position.Line.Should().Be(0);
                notifications[0].Description.Should().BeEmpty();

                notifications[1].Description.Should().Be("DES");
                notifications[1].Code.Should().BeEmpty();
                notifications[1].Title.Should().BeEmpty();
                notifications[1].Position.Offset.Should().Be(11);
                notifications[1].Position.Column.Should().Be(0);
                notifications[1].Position.Line.Should().Be(0);
            }

            [Fact]
            public void ShouldCollectSimplePlan()
            {
                var builder = new ResultBuilder();
                builder.IsStreamingRecords(false);
                IDictionary<string, object> meta = new Dictionary<string, object>
                {   {"type", "r" },
                    { "plan", new Dictionary<string, object>
                {
                    {"operatorType", "X"}
                } } };
                builder.CollectSummaryMeta(meta);

                var result = builder.Build();
                result.Consume();
                var plan = result.Summary.Plan;
                plan.OperatorType.Should().Be("X");
                plan.Arguments.Should().BeEmpty();
                plan.Children.Should().BeEmpty();
                plan.Identifiers.Should().BeEmpty();

            }

            [Fact]
            public void ShouldCollectPlanThatContainsPlans()
            {
                var builder = new ResultBuilder();
                builder.IsStreamingRecords(false);
                IDictionary<string, object> meta = new Dictionary<string, object>
                {
                    {"type", "r"},
                    {
                        "plan", new Dictionary<string, object>
                        {
                            {"operatorType", "X"},
                            {"args", new Dictionary<string, object> {{"a", 1}, {"b", "lala"}}},
                            {"identifiers", new List<object> {"id1", "id2"}},
                            {
                                "children", new List<object>
                                {
                                    new Dictionary<string, object>
                                    {
                                        {"operatorType", "tt"},
                                        {"children", new List<object>()}
                                    },
                                    new Dictionary<string, object>
                                    {
                                        { "operatorType", "Z" },
                                        {
                                            "children", new List<object>
                                            {
                                                new Dictionary<string, object> {{"operatorType", "Y"}}
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                };
                builder.CollectSummaryMeta(meta);

                var result = builder.Build();
                result.Consume();
                var plan = result.Summary.Plan;
                plan.OperatorType.Should().Be("X");
                plan.Arguments.Should().ContainKey("a");
                plan.Arguments["a"].Should().Be(1);
                plan.Arguments.Should().ContainKey("b");
                plan.Arguments["b"].Should().Be("lala");
                plan.Identifiers.Should().ContainInOrder("id1", "id2");
                plan.Children.Should().NotBeNull();
                var children = plan.Children;
                children.Should().HaveCount(2);
                children[0].OperatorType.Should().Be("tt");
                children[0].Children.Should().BeEmpty();
                children[1].Children.Should().HaveCount(1);
                children[1].Children[0].OperatorType.Should().Be("Y");
            }

            [Fact]
            public void ShouldCollectProfiledPlanThatContainsProfiledPlans()
            {
                var builder = new ResultBuilder();
                builder.IsStreamingRecords(false);
                IDictionary<string, object> meta = new Dictionary<string, object>
                {
                    {"type", "r"},
                    {
                        "profile", new Dictionary<string, object>
                        {
                            {"operatorType", "X"},
                            {"args", new Dictionary<string, object> {{"a", 1}, {"b", "lala"}}},
                            {"dbHits", 1L},
                            {"rows", 1L},
                            {"identifiers", new List<object> {"id1", "id2"}},
                            {
                                "children", new List<object>
                                {
                                    new Dictionary<string, object>
                                    {
                                        {"dbHits", 1L},
                                        {"rows", 1L},
                                        {"operatorType", "tt"},
                                        {"children", new List<object>()}
                                    },
                                    new Dictionary<string, object>
                                    {
                                        {"dbHits", 1L},
                                        {"rows", 1L},
                                        {"operatorType", "Z"},
                                        {
                                            "children", new List<object>
                                            {
                                                new Dictionary<string, object>
                                                {
                                                    {"dbHits", 1L},
                                                    {"rows", 1L},
                                                    { "operatorType", "Y"}
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                };
                builder.CollectSummaryMeta(meta);

                var result = builder.Build();
                result.Consume();
                var profile = result.Summary.Profile;
                profile.DbHits.Should().Be(1L);
                profile.OperatorType.Should().Be("X");
                profile.Arguments.Should().ContainKey("a");
                profile.Arguments["a"].Should().Be(1);
                profile.Arguments.Should().ContainKey("b");
                profile.Arguments["b"].Should().Be("lala");
                profile.Identifiers.Should().ContainInOrder("id1", "id2");
                profile.Children.Should().NotBeNull();
                var children = profile.Children;
                children.Should().HaveCount(2);
                children[0].OperatorType.Should().Be("tt");
                children[0].Children.Should().BeEmpty();
                children[1].Children.Should().HaveCount(1);
                children[1].Children[0].OperatorType.Should().Be("Y");
            }
        }

        public class BuildMethod
        {
            private static ResultBuilder GenerateBuilder(IDictionary<string, object> meta = null)
            {
                var builder = new ResultBuilder();
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

            [Fact]
            public void ShouldStreamResults()
            {
                var builder = GenerateBuilder();
                var i = 0;
                builder.ReceiveOneRecordMessageFunc = () =>
                {
                    if (i++ >= 3)
                    {
                        builder.CollectSummaryMeta(null);
                    }
                    builder.CollectRecord(new object[] { 123 });
                };
                var cursor = builder.Build();

                var t = AssertGetExpectResults(cursor, 3);
                t.Wait();
            }

            [Fact]
            public void ShouldReturnNoResultsWhenNoneRecieved()
            {
                var builder = GenerateBuilder();
                builder.ReceiveOneRecordMessageFunc = () =>
                {
                    builder.CollectSummaryMeta(null);
                };
                var cursor = builder.Build();

                var t = AssertGetExpectResults(cursor, 0);

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
                var i = 0;
                builder.ReceiveOneRecordMessageFunc = () =>
                {
                    if (i < recordValues.Count)
                    {
                        builder.CollectRecord(new[] { recordValues[i++] });
                    }
                    builder.CollectSummaryMeta(null);
                };
                var cursor = builder.Build();

                var task = AssertGetExpectResults(cursor, recordValues.Count, recordValues);
                task.Wait();
            }
        }

        public class CollectSummaryMetaMethod
        {
            private static ICounters DefaultCounters => new Counters();

            [Fact]
            public void DoesNothingWhenMetaIsNull()
            {
                var builder = new ResultBuilder();
                builder.ReceiveOneRecordMessageFunc = () =>
                {
                    builder.CollectSummaryMeta(null);
                };
                var actual = builder.Build();
                actual.Consume();

                actual.Summary.HasPlan.Should().BeFalse();
                actual.Summary.HasProfile.Should().BeFalse();
                actual.Summary.Notifications.Should().BeEmpty();
                actual.Summary.Plan.Should().BeNull();
                actual.Summary.Profile.Should().BeNull();
                actual.Summary.Statement.Text.Should().BeNull();
                actual.Summary.StatementType.Should().Be(StatementType.Unknown);
                actual.Summary.Counters.ShouldBeEquivalentTo(DefaultCounters);
            }

            public class TypeMeta
            {
                [Theory]
                [InlineData("r", StatementType.ReadOnly)]
                [InlineData("rw", StatementType.ReadWrite)]
                [InlineData("w", StatementType.WriteOnly)]
                [InlineData("s", StatementType.SchemaWrite)]
                public void ShouldCollectTypeDataWhenSupplied(string typeValue, StatementType expected)
                {
                    var builder = new ResultBuilder();
                    var meta = new Dictionary<string, object>
                    {
                        {"type", typeValue}
                    };

                    builder.ReceiveOneRecordMessageFunc = () =>
                    {
                        builder.CollectSummaryMeta(meta);
                    };
                    var actual = builder.Build();
                    actual.Consume();

                    actual.Summary.StatementType.Should().Be(expected);
                }

                [Fact]
                public void DoesNothingWhenTypeIsNotInMeta()
                {
                    var builder = new ResultBuilder();
                    var meta = new Dictionary<string, object>
                    {
                        {"something", "unknown"}
                    };

                    builder.ReceiveOneRecordMessageFunc = () =>
                    {
                        builder.CollectSummaryMeta(meta);
                    };
                    var actual = builder.Build();
                    actual.Consume();
                    actual.Summary.StatementType.Should().Be(StatementType.Unknown);
                }

                [Fact]
                public void ShouldThrowClientExceptionWhenTypeIsUnknown()
                {
                    var builder = new ResultBuilder();
                    var meta = new Dictionary<string, object>
                    {
                        {"type", "unknown"}
                    };

                    var ex = Xunit.Record.Exception(() => builder.CollectSummaryMeta(meta));
                    ex.Should().BeOfType<ClientException>();
                }
            }

            public class CountersMeta
            {
                [Fact]
                public void DoesNothingWhenStatsIsNotInMeta()
                {
                    var builder = new ResultBuilder();
                    var meta = new Dictionary<string, object>
                    {
                        {"something", "unknown"}
                    };

                    builder.ReceiveOneRecordMessageFunc = () =>
                    {
                        builder.CollectSummaryMeta(meta);
                    };
                    var actual = builder.Build();
                    actual.Consume();
                    actual.Summary.Counters.ShouldBeEquivalentTo(DefaultCounters);
                }

                [Fact]
                public void SetsAllPropertiesCorrectly()
                {
                    var builder = new ResultBuilder();
                    var meta = new Dictionary<string, object>
                    {
                        {"stats", new Dictionary<string, object>
                        {
                            {"nodes-created", 1L },
                            {"nodes-deleted", 2L },
                            {"relationships-created", 3L },
                            {"relationships-deleted", 4L },
                            {"properties-set", 5L },
                            {"labels-added", 6L },
                            {"labels-removed", 7L },
                            {"indexes-added", 8L },
                            {"indexes-removed", 9L },
                            {"constraints-added", 10L },
                            {"constraints-removed", 11L },
                        } }
                    };
                    builder.ReceiveOneRecordMessageFunc = () =>
                    {
                        builder.CollectSummaryMeta(meta);
                    };
                    var actual = builder.Build().Consume().Counters;
                    actual.Should().NotBeNull();

                    actual.NodesCreated.Should().Be(1);
                    actual.NodesDeleted.Should().Be(2);
                    actual.RelationshipsCreated.Should().Be(3);
                    actual.RelationshipsDeleted.Should().Be(4);
                    actual.PropertiesSet.Should().Be(5);
                    actual.LabelsAdded.Should().Be(6);
                    actual.LabelsRemoved.Should().Be(7);
                    actual.IndexesAdded.Should().Be(8);
                    actual.IndexesRemoved.Should().Be(9);
                    actual.ConstraintsAdded.Should().Be(10);
                    actual.ConstraintsRemoved.Should().Be(11);
                }
            }

            public class PlanMeta
            {
                [Fact]
                public void DoesNothingWhenPlanIsNotInMeta()
                {
                    var builder = new ResultBuilder();
                    var meta = new Dictionary<string, object>
                    {
                        {"something", "unknown"}
                    };
                    builder.ReceiveOneRecordMessageFunc = () =>
                    {
                        builder.CollectSummaryMeta(meta);
                    };
                    var actual = builder.Build();
                    actual.Consume();

                    actual.Summary.Plan.Should().BeNull();
                    actual.Summary.HasPlan.Should().BeFalse();
                }

                [Fact]
                public void DoesNothingWhenPlanIsThereButHasNoContent()
                {
                    var builder = new ResultBuilder();
                    var meta = new Dictionary<string, object>
                    {
                        {"plan", new Dictionary<string,object>()}
                    };

                    builder.ReceiveOneRecordMessageFunc = () =>
                    {
                        builder.CollectSummaryMeta(meta);
                    };
                    var actual = builder.Build();
                    actual.Consume();

                    actual.Summary.Plan.Should().BeNull();
                    actual.Summary.HasPlan.Should().BeFalse();
                }

                [Fact]
                public void DoesNothingWhenPlanIsThereButIsNull()
                {
                    var builder = new ResultBuilder();
                    var meta = new Dictionary<string, object>
                    {
                        {"plan", null}
                    };

                    builder.ReceiveOneRecordMessageFunc = () =>
                    {
                        builder.CollectSummaryMeta(meta);
                    };
                    var actual = builder.Build();
                    actual.Consume();

                    actual.Summary.Plan.Should().BeNull();
                    actual.Summary.HasPlan.Should().BeFalse();
                }

                [Fact]
                public void ShouldThrowNeo4jExceptionWhenOperatorTypeIsNotSupplied()
                {
                    var builder = new ResultBuilder();
                    var meta = new Dictionary<string, object>
                    {
                        {
                            "plan", new Dictionary<string, object>
                            {
                                {"args", new Dictionary<string, object>() }
                            }
                        }
                    };

                    builder.ReceiveOneRecordMessageFunc = () =>
                    {
                        builder.CollectSummaryMeta(meta);
                    };
                    var actual = builder.Build();

                    var ex = Xunit.Record.Exception(() => actual.Consume());
                    ex.Should().BeOfType<Neo4jException>();
                    ex.Message.Should().Be("Required property 'operatorType' is not in the response.");
                }

                [Fact]
                public void ShouldUseDefaultValuesWhenPlanIsThereButHasNoData()
                {
                    var builder = new ResultBuilder();
                    var meta = new Dictionary<string, object>
                    {
                        {
                            "plan", new Dictionary<string, object>
                            {
                                {"operatorType", "opType"}
                            }
                        }
                    };

                    builder.ReceiveOneRecordMessageFunc = () =>
                    {
                        builder.CollectSummaryMeta(meta);
                    };
                    var actual = builder.Build();
                    actual.Consume();

                    actual.Summary.Plan.Should().NotBeNull();
                    actual.Summary.HasPlan.Should().BeTrue();
                    
                    var plan = actual.Summary.Plan;
                    plan.Arguments.Should().BeEmpty();

                    plan.OperatorType.Should().Be("opType");
                    plan.Identifiers.Should().BeEmpty();
                    plan.Children.Should().BeEmpty();
                }

                [Fact]
                public void ShouldSetValuesWhenThereAreNoChildPlans()
                {
                    var builder = new ResultBuilder();
                    var meta = new Dictionary<string, object>
                    {
                        {
                            "plan", new Dictionary<string, object>
                            {
                                {"operatorType", "opType"},
                                {"args", new Dictionary<string, object> { {"a", 1} } },
                                {"identifiers", new List<object> {"a", "b" } }
                            }
                        }
                    };

                    builder.ReceiveOneRecordMessageFunc = () =>
                    {
                        builder.CollectSummaryMeta(meta);
                    };
                    var actual = builder.Build();
                    actual.Consume();

                    actual.Summary.Plan.Should().NotBeNull();
                    actual.Summary.HasPlan.Should().BeTrue();

                    var plan = actual.Summary.Plan;
                    plan.Arguments.Should().HaveCount(1);
                    plan.Arguments.Should().ContainKey("a");
                    plan.Arguments["a"].Should().Be(1);

                    plan.OperatorType.Should().Be("opType");

                    plan.Identifiers.Should().HaveCount(2);
                    plan.Identifiers.Should().ContainInOrder("a", "b");

                    plan.Children.Should().BeEmpty();
                }

                [Fact]
                public void ShouldSetChildrenPlans()
                {
                    var builder = new ResultBuilder();
                    var meta = new Dictionary<string, object>
                    {
                        {
                            "plan", new Dictionary<string, object>
                            {
                                {"operatorType", "opType"},
                                {"args", new Dictionary<string, object> {{"a", 1}}},
                                {"identifiers", new List<object> {"a", "b"}},
                                {
                                    "children", new List<object>
                                    {
                                        new Dictionary<string, object>
                                        {
                                            {"operatorType", "childOpType"},
                                            {"args", new Dictionary<string, object> {{"child_a", 2}}},
                                            {"identifiers", new List<object> {"child_a", "child_b"}}
                                        }
                                    }
                                }
                            }
                        }
                    };

                    builder.ReceiveOneRecordMessageFunc = () =>
                    {
                        builder.CollectSummaryMeta(meta);
                    };
                    var actual = builder.Build().Consume().Plan.Children.Single();

                    actual.Arguments.Should().HaveCount(1);
                    actual.Arguments.Should().ContainKey("child_a");
                    actual.Arguments["child_a"].Should().Be(2);
                    
                    actual.OperatorType.Should().Be("childOpType");
                
                    actual.Identifiers.Should().HaveCount(2);
                    actual.Identifiers.Should().ContainInOrder("child_a", "child_b");
            
                    actual.Children.Should().BeEmpty();
                }

                [Fact]
                public void ShouldSetNestedChildrenPlans()
                {
                    var builder = new ResultBuilder();
                    var meta = new Dictionary<string, object>
                    {
                        {
                            "plan", new Dictionary<string, object>
                            {
                                {"operatorType", "opType"},
                                {"args", new Dictionary<string, object> {{"a", 1}}},
                                {"identifiers", new List<object> {"a", "b"}},
                                {
                                    "children", new List<object>
                                    {
                                        new Dictionary<string, object>
                                        {
                                            {"operatorType", "childOpType"},
                                            {"args", new Dictionary<string, object> {{"child_a", 2}}},
                                            {"identifiers", new List<object> {"child_a", "child_b"}},
                                            {
                                                "children", new List<object>
                                                {
                                                    new Dictionary<string, object>
                                                    {
                                                        {"operatorType", "childChildOpType"},
                                                        {"args", new Dictionary<string, object> {{"childChild_a", 3}}},
                                                        {"identifiers", new List<object> {"childChild_a", "childChild_b"}}
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    };

                    builder.ReceiveOneRecordMessageFunc = () =>
                    {
                        builder.CollectSummaryMeta(meta);
                    };
                    var actual = builder.Build().Consume().Plan.Children.Single().Children.Single();

                    actual.Arguments.Should().HaveCount(1);
                    actual.Arguments.Should().ContainKey("childChild_a");
                    actual.Arguments["childChild_a"].Should().Be(3);

                    actual.OperatorType.Should().Be("childChildOpType");

                    actual.Identifiers.Should().HaveCount(2);
                    actual.Identifiers.Should().ContainInOrder("childChild_a", "childChild_b");

                    actual.Children.Should().BeEmpty();
                }
            }

            public class ProfileMeta
            {
                [Fact]
                public void DoesNothingWhenProfileIsNotInMeta()
                {
                    var builder = new ResultBuilder();
                    var meta = new Dictionary<string, object>
                    {
                        {"something", "unknown"}
                    };

                    builder.ReceiveOneRecordMessageFunc = () =>
                    {
                        builder.CollectSummaryMeta(meta);
                    };
                    var actual = builder.Build();
                    actual.Consume();

                    actual.Summary.Profile.Should().BeNull();
                    actual.Summary.HasProfile.Should().BeFalse();
                }

                [Fact]
                public void DoesNothingWhenProfileIsThereButHasNoContent()
                {
                    var builder = new ResultBuilder();
                    var meta = new Dictionary<string, object>
                    {
                        {"profile", new Dictionary<string,object>()}
                    };

                    builder.ReceiveOneRecordMessageFunc = () =>
                    {
                        builder.CollectSummaryMeta(meta);
                    };
                    var actual = builder.Build();
                    actual.Consume();

                    actual.Summary.Profile.Should().BeNull();
                    actual.Summary.HasProfile.Should().BeFalse();
                }

                [Fact]
                public void DoesNothingWhenProfileIsThereButIsNull()
                {
                    var builder = new ResultBuilder();
                    var meta = new Dictionary<string, object>
                    {
                        {"profile", null}
                    };

                    builder.ReceiveOneRecordMessageFunc = () =>
                    {
                        builder.CollectSummaryMeta(meta);
                    };
                    var actual = builder.Build();
                    actual.Consume();

                    actual.Summary.Profile.Should().BeNull();
                    actual.Summary.HasProfile.Should().BeFalse();
                }


                [Fact]
                public void ShouldThrowNeo4jExceptionWhenOperatorTypeIsNotSupplied()
                {
                    var builder = new ResultBuilder();
                    var meta = new Dictionary<string, object>
                    {
                        {
                            "profile", new Dictionary<string, object>
                               {
                                {"dbHits", 1L },
                                {"rows", 2L }
                            }
                        }
                    };

                    builder.ReceiveOneRecordMessageFunc = () =>
                    {
                        builder.CollectSummaryMeta(meta);
                    };
                    var actual = builder.Build();

                    var ex = Xunit.Record.Exception(() => actual.Consume());
                    ex.Should().BeOfType<Neo4jException>();
                    ex.Message.Should().Be("Required property 'operatorType' is not in the response.");
                }

                [Fact]
                public void ShouldThrowNeo4jExceptionWhenRecordsIsNotSupplied()
                {
                    var builder = new ResultBuilder();
                    var meta = new Dictionary<string, object>
                    {
                        {
                            "profile", new Dictionary<string, object>
                              {
                                {"operatorType", "valid" },
                                {"dbHits", 2L }
                            }
                        }
                    };

                    builder.ReceiveOneRecordMessageFunc = () =>
                    {
                        builder.CollectSummaryMeta(meta);
                    };
                    var actual = builder.Build();

                    var ex = Xunit.Record.Exception(() => actual.Consume());
                    ex.Should().BeOfType<Neo4jException>();
                    ex.Message.Should().Be("Required property 'rows' is not in the response.");
                }

                [Fact]
                public void ShouldThrowNeo4jExceptionWhenDbHitsIsNotSupplied()
                {
                    var builder = new ResultBuilder();
                    var meta = new Dictionary<string, object>
                    {
                        {
                            "profile", new Dictionary<string, object>
                            {
                                {"operatorType", "valid" },
                                {"rows", 2L }
                            }
                        }
                    };

                    builder.ReceiveOneRecordMessageFunc = () =>
                    {
                        builder.CollectSummaryMeta(meta);
                    };
                    var actual = builder.Build();

                    var ex = Xunit.Record.Exception(() => actual.Consume());
                    ex.Should().BeOfType<Neo4jException>();
                    ex.Message.Should().Be("Required property 'dbHits' is not in the response.");
                }

                [Fact]
                public void ShouldUseDefaultValuesWhenProfileIsThereButHasNoData()
                {
                    var builder = new ResultBuilder();
                    var meta = new Dictionary<string, object>
                    {
                        {
                            "profile", new Dictionary<string, object>
                            {
                                {"operatorType", "opType"},
                                {"rows", 1L },
                                {"dbHits", 2L },
                            }
                        }
                    };

                    builder.ReceiveOneRecordMessageFunc = () =>
                    {
                        builder.CollectSummaryMeta(meta);
                    };
                    var actual = builder.Build();
                    actual.Consume();

                    actual.Summary.Profile.Should().NotBeNull();
                    actual.Summary.HasProfile.Should().BeTrue();

                    var profile = actual.Summary.Profile;
                    profile.Arguments.Should().BeEmpty();

                    profile.OperatorType.Should().Be("opType");
                    profile.Identifiers.Should().BeEmpty();
                    profile.Children.Should().BeEmpty();

                    profile.Records.Should().Be(1L);
                    profile.DbHits.Should().Be(2L);
                }

                [Fact]
                public void ShouldUseGivenValuesWhenProfileHasData()
                {
                    var builder = new ResultBuilder();
                    var meta = new Dictionary<string, object>
                    {
                        {
                            "profile", new Dictionary<string, object>
                            {
                                {"operatorType", "opType"},
                                {"args", new Dictionary<string, object> { {"a", 1} } },
                                {"identifiers", new List<object> {"a", "b" } },
                                {"dbHits", 1L },
                                {"rows", 2L }
                            }
                        }
                    };

                    builder.ReceiveOneRecordMessageFunc = () =>
                    {
                        builder.CollectSummaryMeta(meta);
                    };
                    var actual = builder.Build();
                    actual.Consume();
                    actual.Summary.Profile.Should().NotBeNull();
                    actual.Summary.HasProfile.Should().BeTrue();

                    var profile = actual.Summary.Profile;

                    profile.Arguments.Should().HaveCount(1);
                    profile.Arguments.Should().ContainKey("a");
                    profile.Arguments["a"].Should().Be(1);

                    profile.OperatorType.Should().Be("opType");

                    profile.Identifiers.Should().HaveCount(2);
                    profile.Identifiers.Should().ContainInOrder("a", "b");

                    profile.Children.Should().BeEmpty();

                    profile.DbHits.Should().Be(1L);
                    profile.Records.Should().Be(2L);
                }

                [Fact]
                public void ShouldUseSetChildEntriesWhenSupplied()
                {
                    var builder = new ResultBuilder();
                    var meta = new Dictionary<string, object>
                    {
                        {
                            "profile", new Dictionary<string, object>
                            {
                                {"operatorType", "opType"},
                                {"args", new Dictionary<string, object> {{"a", 1}}},
                                {"identifiers", new List<object> {"a", "b"}},
                                {"dbHits", 1L},
                                {"rows", 2L},
                                {
                                    "children", new List<object>
                                    {
                                        new Dictionary<string, object>
                                        {
                                            {"operatorType", "childOpType"},
                                            {"args", new Dictionary<string, object> {{"child_a", 2}}},
                                            {"identifiers", new List<object> { "child_a", "child_b"}},
                                            {"dbHits", 3L},
                                            {"rows", 4L}
                                        }
                                    }
                                }
                            }
                        }
                    };

                    builder.ReceiveOneRecordMessageFunc = () =>
                    {
                        builder.CollectSummaryMeta(meta);
                    };
                    var actual = builder.Build();
                    actual.Consume();
                    actual.Summary.Profile.Should().NotBeNull();
                    actual.Summary.HasProfile.Should().BeTrue();

                    var profile = actual.Summary.Profile.Children.Single();

                    profile.Arguments.Should().HaveCount(1);
                    profile.Arguments.Should().ContainKey("child_a");
                    profile.Arguments["child_a"].Should().Be(2);

                    profile.OperatorType.Should().Be("childOpType");

                    profile.Identifiers.Should().HaveCount(2);
                    profile.Identifiers.Should().ContainInOrder("child_a", "child_b");

                    profile.Children.Should().BeEmpty();

                    profile.DbHits.Should().Be(3L);
                    profile.Records.Should().Be(4L);
                }

                [Fact]
                public void ShouldUseSetChildEntriesWhenNestedChildrenSupplied()
                {
                    var builder = new ResultBuilder();
                    var meta = new Dictionary<string, object>
                    {
                        {
                            "profile", new Dictionary<string, object>
                            {
                                {"operatorType", "opType"},
                                {"args", new Dictionary<string, object> {{"a", 1}}},
                                {"identifiers", new List<object> {"a", "b"}},
                                {"dbHits", 1L},
                                {"rows", 2L},
                                {
                                    "children", new List<object>
                                    {
                                        new Dictionary<string, object>
                                        {
                                            {"operatorType", "childOpType"},
                                            {"args", new Dictionary<string, object> {{"child_a", 2}}},
                                            {"identifiers", new List<object> {"child_a", "child_b"}},
                                            {"dbHits", 3L},
                                            {"rows", 4L},
                                            {
                                                "children", new List<object>
                                                {
                                                    new Dictionary<string, object>
                                                    {
                                                        {"operatorType", "childChildOpType"},
                                                        {"args", new Dictionary<string, object> {{ "childChild_a", 3}}},
                                                        {"identifiers", new List<object> { "childChild_a", "childChild_b"}},
                                                        {"dbHits", 5L},
                                                        {"rows", 6L}
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    };

                    builder.ReceiveOneRecordMessageFunc = () =>
                    {
                        builder.CollectSummaryMeta(meta);
                    };
                    var actual = builder.Build();
                    actual.Consume();
                    actual.Summary.Profile.Should().NotBeNull();
                    actual.Summary.HasProfile.Should().BeTrue();

                    var profile = actual.Summary.Profile.Children.Single().Children.Single();

                    profile.Arguments.Should().HaveCount(1);
                    profile.Arguments.Should().ContainKey("childChild_a");
                    profile.Arguments["childChild_a"].Should().Be(3);

                    profile.OperatorType.Should().Be("childChildOpType");

                    profile.Identifiers.Should().HaveCount(2);
                    profile.Identifiers.Should().ContainInOrder("childChild_a", "childChild_b");

                    profile.Children.Should().BeEmpty();

                    profile.DbHits.Should().Be(5L);
                    profile.Records.Should().Be(6L);
                }
            }

            public class NotificationsMeta
            {
                [Fact]
                public void DoesNothingWhenNotificationsIsNotInMeta()
                {
                    var builder = new ResultBuilder();
                    var meta = new Dictionary<string, object>
                    {
                        {"something", "unknown"}
                    };

                    builder.ReceiveOneRecordMessageFunc = () =>
                    {
                        builder.CollectSummaryMeta(meta);
                    };
                    var actual = builder.Build();
                    actual.Consume();
                    actual.Summary.Notifications.Should().BeEmpty();
                }

                [Fact]
                public void ShouldSetSingleNotificationWhenReturned()
                {
                    var builder = new ResultBuilder();
                    var meta = new Dictionary<string, object>
                    {
                        {
                            "notifications", new List<object>
                            {
                                new Dictionary<string, object>
                                {
                                    {"code", "c"},
                                    {"title", "t"},
                                    {"description", "d"},
                                    {
                                        "position", new Dictionary<string, object>
                                        {
                                            {"offset", 1L},
                                            {"line", 2L},
                                            {"column", 3L}
                                        }
                                    }
                                }
                            }
                        }
                    };

                    builder.ReceiveOneRecordMessageFunc = () =>
                    {
                        builder.CollectSummaryMeta(meta);
                    };
                    var actual = builder.Build();
                    actual.Consume();
                    actual.Summary.Notifications.Should().HaveCount(1);

                    var n = actual.Summary.Notifications.Single();
                    n.Code.Should().Be("c");
                    n.Title.Should().Be("t");
                    n.Description.Should().Be("d");
                    n.Position.Offset.Should().Be(1);
                    n.Position.Line.Should().Be(2);
                    n.Position.Column.Should().Be(3);
                }

                [Fact]
                public void ShouldCopeWithMultipleNotifications()
                {
                    var builder = new ResultBuilder();
                    var meta = new Dictionary<string, object>
                    {
                        {
                            "notifications", new List<object>
                            {
                                new Dictionary<string, object>
                                {
                                    {"code", "c"},
                                    {"title", "t"},
                                    {"description", "d"},
                                    {
                                        "position", new Dictionary<string, object>
                                        {
                                            {"offset", 1L},
                                            {"line", 2L},
                                            {"column", 3L}
                                        }
                                    }
                                },
                                new Dictionary<string, object>
                                {
                                    {"code", "c2"},
                                    {"title", "t2"},
                                    {"description", "d2"},
                                    {
                                        "position", new Dictionary<string, object>
                                        {
                                            {"offset", 4L},
                                            {"line", 5L},
                                            {"column", 6L}
                                        }
                                    }
                                }
                            }
                        }
                    };

                    builder.ReceiveOneRecordMessageFunc = () =>
                    {
                        builder.CollectSummaryMeta(meta);
                    };
                    var actual = builder.Build();
                    actual.Consume();
                    actual.Summary.Notifications.Should().HaveCount(2);

                    var n = actual.Summary.Notifications.Skip(1).Single();
                    n.Code.Should().Be("c2");
                    n.Title.Should().Be("t2");
                    n.Description.Should().Be("d2");
                    n.Position.Offset.Should().Be(4);
                    n.Position.Line.Should().Be(5);
                    n.Position.Column.Should().Be(6);
                }
            }
        }

        public class CollectFieldsMethod
        {
            [Fact]
            public void ShouldDoNothingWhenMetaIsNull()
            {
                var builder = new ResultBuilder();
                builder.CollectFields(null);

                var actual = builder.Build();
                actual.Keys.Should().BeEmpty();
            }

            [Fact]
            public void ShouldDoNothingWhenMetaDoesNotContainFields()
            {
                var builder = new ResultBuilder();
                var meta = new Dictionary<string, object>
                {
                    {"something", "here" }
                };
                builder.CollectFields(meta);

                var actual = builder.Build();
                actual.Keys.Should().BeEmpty();
            }
        }
    }
}
