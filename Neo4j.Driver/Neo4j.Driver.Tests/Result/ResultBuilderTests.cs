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
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.V1;
using Xunit;

namespace Neo4j.Driver.Tests
{
    public class ResultBuilderTests
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

        public class CollectRecordMethod
        {
            [Fact]
            public void ShouldStreamResults()
            {
                var builder = GenerateBuilder();
                var i = 0;
                builder.SetReceiveOneFunc(() =>
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
                builder.SetReceiveOneFunc(() =>
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
        }

        public class CollectSummaryMethod
        {
            private static ICounters DefaultCounters => new Counters();

            [Fact]
            public void DoesNothingWhenSummaryIsNull()
            {
                var builder = new ResultBuilder();
                builder.CollectSummary(null);
                var actual = builder.PreBuild();
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

                    builder.CollectSummary(meta);
                    var actual = builder.PreBuild();
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

                    builder.CollectSummary(meta);
                    var actual = builder.PreBuild();
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

                    var ex = Xunit.Record.Exception(() => builder.CollectSummary(meta));
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

                    builder.CollectSummary(meta);
                    var actual = builder.PreBuild();
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
                    builder.CollectSummary(meta);
                    var actual = builder.PreBuild().Consume().Counters;
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
                    builder.CollectSummary(meta);
                    var actual = builder.PreBuild();
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

                    builder.CollectSummary(meta);
                    var actual = builder.PreBuild();
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

                    builder.CollectSummary(meta);
                    var actual = builder.PreBuild();
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

                   ;

                    var ex = Xunit.Record.Exception(() => builder.CollectSummary(meta));
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

                    builder.CollectSummary(meta);
                    var actual = builder.PreBuild();
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

                    builder.CollectSummary(meta);
                    var actual = builder.PreBuild();
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

                    builder.CollectSummary(meta);
                    var actual = builder.PreBuild().Consume().Plan.Children.Single();

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

                    builder.CollectSummary(meta);
                    var actual = builder.PreBuild().Consume().Plan.Children.Single().Children.Single();

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

                    builder.CollectSummary(meta);
                    var actual = builder.PreBuild();
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

                    builder.CollectSummary(meta);
                    var actual = builder.PreBuild();
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

                    builder.CollectSummary(meta);
                    var actual = builder.PreBuild();
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

                    var ex = Xunit.Record.Exception(() => builder.CollectSummary(meta));
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

                    var ex = Xunit.Record.Exception(() => builder.CollectSummary(meta));
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

                    var ex = Xunit.Record.Exception(() => builder.CollectSummary(meta));
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

                    builder.CollectSummary(meta);
                    var actual = builder.PreBuild();
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

                    builder.CollectSummary(meta);
                    var actual = builder.PreBuild();
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
                    builder.CollectSummary(meta);
                    var actual = builder.PreBuild();
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
                    
                    builder.CollectSummary(meta);
                    var actual = builder.PreBuild();
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

                    builder.CollectSummary(meta);
                    var actual = builder.PreBuild();
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

                    builder.CollectSummary(meta);
                    var actual = builder.PreBuild();
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

                    builder.CollectSummary(meta);
                    var actual = builder.PreBuild();
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
            public void ShouldPassDefaultKeysToResultIfNoKeySet()
            {
                var builder = new ResultBuilder();
                var result = builder.PreBuild();

                result.Keys.Should().BeEmpty();
            }

            [Fact]
            public void ShouldDoNothingWhenMetaIsNull()
            {
                var builder = new ResultBuilder();
                builder.CollectFields(null);

                var result = builder.PreBuild();
                result.Keys.Should().BeEmpty();
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

                var result = builder.PreBuild();
                result.Keys.Should().BeEmpty();
            }

            [Fact]
            public void ShouldCollectKeys()
            {
                IDictionary<string, object> meta = new Dictionary<string, object>
                { {"fields", new List<object> {"fieldKey1", "fieldKey2", "fieldKey3"} },{"type", "r" } };

                var builder = new ResultBuilder();
                builder.CollectFields(meta);
                var result = builder.PreBuild();

                result.Keys.Should().ContainInOrder("fieldKey1", "fieldKey2", "fieldKey3");
            }
        }

        public class InvalidateResultMethod
        {
            [Fact]
            public void ShouldStopStreamingWhenResultIsInvalid()
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
                        builder.CollectRecord(new object[] {123 + i});
                    }
                });
                var result = builder.PreBuild();

                var t = AssertGetExpectResults(result, 3, new List<object> { 124, 125, 126 });
                t.Wait();
            }
        }
    }
}
