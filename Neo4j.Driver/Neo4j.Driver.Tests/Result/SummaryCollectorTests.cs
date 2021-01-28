// Copyright (c) "Neo4j"
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
using Moq;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.V1;
using Xunit;
using Record = Xunit.Record;

namespace Neo4j.Driver.Tests
{
    public class SummaryCollectorTests
    {
        private static ICounters DefaultCounters => new Counters();

        private static SummaryCollector NewSummaryCollector()
        {
            return new SummaryCollector(new Statement(null), new Mock<IServerInfo>().Object);
        }

        public class CollectWithFields
        {
            [Fact]
            public void ShouldCollectAllItems()
            {
                var mock = new Mock<IDictionary<string, object>>();
                var collector = NewSummaryCollector();
                collector.CollectWithFields(mock.Object);

                mock.Verify(x => x.ContainsKey("result_available_after"), Times.Once);
            }
        }

        public class CollectMethod
        {
            [Fact]
            public void ShouldCollectAllItems()
            {
                var mock = new Mock<IDictionary<string, object>>();
                var collector = NewSummaryCollector();
                collector.Collect(mock.Object);

                mock.Verify(x=>x.ContainsKey("type"), Times.Once);
                mock.Verify(x=>x.ContainsKey("stats"), Times.Once);
                mock.Verify(x=>x.ContainsKey("plan"), Times.Once);
                mock.Verify(x=>x.ContainsKey("profile"), Times.Once);
                mock.Verify(x=>x.ContainsKey("notifications"), Times.Once);
                mock.Verify(x=>x.ContainsKey("result_consumed_after"), Times.Once);
            }
        }

        public class CollectBookmarkMethod
        {
            [Fact]
            public void ShouldThrowExceptionWhenTryingToCollectBookmark()
            {
                var collector = NewSummaryCollector();
                var error = Record.Exception(() =>
                    collector.CollectBookmark(new Dictionary<string, object> {{"bookmark", "I shall not be here"}}));
                error.Should().BeOfType<NotSupportedException>();
                error.Message.Should().Contain("not get a bookmark on a result");
            }
        }

        public class BuildMethod
        {
            [Fact]
            public void ShouldBuildEmptySummary()
            {
                var collector = NewSummaryCollector();
                var summary = collector.Build();

                summary.HasPlan.Should().BeFalse();
                summary.HasProfile.Should().BeFalse();
                summary.Notifications.Should().BeEmpty();
                summary.Plan.Should().BeNull();
                summary.Profile.Should().BeNull();
                summary.Statement.Text.Should().BeNull();
                summary.StatementType.Should().Be(StatementType.Unknown);
                summary.Counters.ShouldBeEquivalentTo(DefaultCounters);
                summary.ResultAvailableAfter.ToString().Should().Be("-00:00:00.0010000");
                summary.ResultConsumedAfter.ToString().Should().Be("-00:00:00.0010000");
            }
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
                var collector = NewSummaryCollector();
                var meta = new Dictionary<string, object>
                {
                    {"type", typeValue}
                };

                collector.Collect(meta);
                var summary = collector.Build();

                summary.StatementType.Should().Be(expected);
            }

            [Fact]
            public void DoesNothingWhenTypeIsNotInMeta()
            {
                var collector = NewSummaryCollector();
                var meta = new Dictionary<string, object>
                {
                    {"something", "unknown"}
                };

                collector.Collect(meta);
                var summary = collector.Build();
                summary.StatementType.Should().Be(StatementType.Unknown);
            }

            [Fact]
            public void ShouldThrowClientExceptionWhenTypeIsUnknown()
            {
                var collector = NewSummaryCollector();
                var meta = new Dictionary<string, object>
                {
                    {"type", "unknown"}
                };

                var ex = Xunit.Record.Exception(() => collector.Collect(meta));
                ex.Should().BeOfType<ClientException>();
            }
        }

        public class CountersMeta
        {
            [Fact]
            public void DoesNothingWhenStatsIsNotInMeta()
            {
                var collector = NewSummaryCollector();;
                var meta = new Dictionary<string, object>
                {
                    {"something", "unknown"}
                };

                collector.Collect(meta);
                var summary = collector.Build();
                summary.Counters.ShouldBeEquivalentTo(DefaultCounters);
            }

            [Fact]
            public void SetsAllPropertiesCorrectly()
            {
                var collector = NewSummaryCollector();;
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
                collector.Collect(meta);
                var summary = collector.Build().Counters;
                summary.Should().NotBeNull();

                summary.NodesCreated.Should().Be(1);
                summary.NodesDeleted.Should().Be(2);
                summary.RelationshipsCreated.Should().Be(3);
                summary.RelationshipsDeleted.Should().Be(4);
                summary.PropertiesSet.Should().Be(5);
                summary.LabelsAdded.Should().Be(6);
                summary.LabelsRemoved.Should().Be(7);
                summary.IndexesAdded.Should().Be(8);
                summary.IndexesRemoved.Should().Be(9);
                summary.ConstraintsAdded.Should().Be(10);
                summary.ConstraintsRemoved.Should().Be(11);
            }
        }

        public class PlanMeta
        {
            [Fact]
            public void DoesNothingWhenPlanIsNotInMeta()
            {
                var collector = NewSummaryCollector();;
                var meta = new Dictionary<string, object>
                {
                    {"something", "unknown"}
                };
                collector.Collect(meta);
                var summary = collector.Build();

                summary.Plan.Should().BeNull();
                summary.HasPlan.Should().BeFalse();
            }

            [Fact]
            public void DoesNothingWhenPlanIsThereButHasNoContent()
            {
                var collector = NewSummaryCollector();;
                var meta = new Dictionary<string, object>
                {
                    {"plan", new Dictionary<string,object>()}
                };

                collector.Collect(meta);
                var summary = collector.Build();

                summary.Plan.Should().BeNull();
                summary.HasPlan.Should().BeFalse();
            }

            [Fact]
            public void DoesNothingWhenPlanIsThereButIsNull()
            {
                var collector = NewSummaryCollector();;
                var meta = new Dictionary<string, object>
                {
                    {"plan", null}
                };

                collector.Collect(meta);
                var summary = collector.Build();

                summary.Plan.Should().BeNull();
                summary.HasPlan.Should().BeFalse();
            }

            [Fact]
            public void ShouldThrowNeo4jExceptionWhenOperatorTypeIsNotSupplied()
            {
                var collector = NewSummaryCollector();;
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

                var ex = Xunit.Record.Exception(() => collector.Collect(meta));
                ex.Should().BeOfType<Neo4jException>();
                ex.Message.Should().Be("Required property 'operatorType' is not in the response.");
            }

            [Fact]
            public void ShouldUseDefaultValuesWhenPlanIsThereButHasNoData()
            {
                var collector = NewSummaryCollector();;
                var meta = new Dictionary<string, object>
                {
                    {
                        "plan", new Dictionary<string, object>
                        {
                            {"operatorType", "opType"}
                        }
                    }
                };

                collector.Collect(meta);
                var summary = collector.Build();

                summary.Plan.Should().NotBeNull();
                summary.HasPlan.Should().BeTrue();

                var plan = summary.Plan;
                plan.Arguments.Should().BeEmpty();

                plan.OperatorType.Should().Be("opType");
                plan.Identifiers.Should().BeEmpty();
                plan.Children.Should().BeEmpty();
            }

            [Fact]
            public void ShouldSetValuesWhenThereAreNoChildPlans()
            {
                var collector = NewSummaryCollector();;
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

                collector.Collect(meta);
                var summary = collector.Build();

                summary.Plan.Should().NotBeNull();
                summary.HasPlan.Should().BeTrue();

                var plan = summary.Plan;
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
                var collector = NewSummaryCollector();;
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

                collector.Collect(meta);
                var actual = collector.Build().Plan.Children.Single();

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
                var collector = NewSummaryCollector();;
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

                collector.Collect(meta);
                var actual = collector.Build().Plan.Children.Single().Children.Single();

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
                var collector = NewSummaryCollector();;
                var meta = new Dictionary<string, object>
                {
                    {"something", "unknown"}
                };

                collector.Collect(meta);
                var summary = collector.Build();

                summary.Profile.Should().BeNull();
                summary.HasProfile.Should().BeFalse();
            }

            [Fact]
            public void DoesNothingWhenProfileIsThereButHasNoContent()
            {
                var collector = NewSummaryCollector();;
                var meta = new Dictionary<string, object>
                {
                    {"profile", new Dictionary<string,object>()}
                };

                collector.Collect(meta);
                var summary = collector.Build();

                summary.Profile.Should().BeNull();
                summary.HasProfile.Should().BeFalse();
            }

            [Fact]
            public void DoesNothingWhenProfileIsThereButIsNull()
            {
                var collector = NewSummaryCollector();;
                var meta = new Dictionary<string, object>
                {
                    {"profile", null}
                };

                collector.Collect(meta);
                var summary = collector.Build();

                summary.Profile.Should().BeNull();
                summary.HasProfile.Should().BeFalse();
            }


            [Fact]
            public void ShouldThrowNeo4jExceptionWhenOperatorTypeIsNotSupplied()
            {
                var collector = NewSummaryCollector();;
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

                var ex = Xunit.Record.Exception(() => collector.Collect(meta));
                ex.Should().BeOfType<Neo4jException>();
                ex.Message.Should().Be("Required property 'operatorType' is not in the response.");
            }

            [Fact]
            public void ShouldThrowNeo4jExceptionWhenRecordsIsNotSupplied()
            {
                var collector = NewSummaryCollector();;
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

                var ex = Xunit.Record.Exception(() => collector.Collect(meta));
                ex.Should().BeOfType<Neo4jException>();
                ex.Message.Should().Be("Required property 'rows' is not in the response.");
            }

            [Fact]
            public void ShouldThrowNeo4jExceptionWhenDbHitsIsNotSupplied()
            {
                var collector = NewSummaryCollector();;
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

                var ex = Xunit.Record.Exception(() => collector.Collect(meta));
                ex.Should().BeOfType<Neo4jException>();
                ex.Message.Should().Be("Required property 'dbHits' is not in the response.");
            }

            [Fact]
            public void ShouldUseDefaultValuesWhenProfileIsThereButHasNoData()
            {
                var collector = NewSummaryCollector();;
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

                collector.Collect(meta);
                var summary = collector.Build();
                

                summary.Profile.Should().NotBeNull();
                summary.HasProfile.Should().BeTrue();

                var profile = summary.Profile;
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
                var collector = NewSummaryCollector();;
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

                collector.Collect(meta);
                var summary = collector.Build();
                
                summary.Profile.Should().NotBeNull();
                summary.HasProfile.Should().BeTrue();

                var profile = summary.Profile;

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
                var collector = NewSummaryCollector();;
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
                collector.Collect(meta);
                var summary = collector.Build();
                
                summary.Profile.Should().NotBeNull();
                summary.HasProfile.Should().BeTrue();

                var profile = summary.Profile.Children.Single();

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
                var collector = NewSummaryCollector();;
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

                collector.Collect(meta);
                var summary = collector.Build();
                
                summary.Profile.Should().NotBeNull();
                summary.HasProfile.Should().BeTrue();

                var profile = summary.Profile.Children.Single().Children.Single();

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
                var collector = NewSummaryCollector();;
                var meta = new Dictionary<string, object>
                {
                    {"something", "unknown"}
                };

                collector.Collect(meta);
                var summary = collector.Build();
                
                summary.Notifications.Should().BeEmpty();
            }

            [Fact]
            public void ShouldSetSingleNotificationWhenReturned()
            {
                var collector = NewSummaryCollector();;
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

                collector.Collect(meta);
                var summary = collector.Build();
                
                summary.Notifications.Should().HaveCount(1);

                var n = summary.Notifications.Single();
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
                var collector = NewSummaryCollector();;
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

                collector.Collect(meta);
                var summary = collector.Build();
                
                summary.Notifications.Should().HaveCount(2);

                var n = summary.Notifications.Skip(1).Single();
                n.Code.Should().Be("c2");
                n.Title.Should().Be("t2");
                n.Description.Should().Be("d2");
                n.Position.Offset.Should().Be(4);
                n.Position.Line.Should().Be(5);
                n.Position.Column.Should().Be(6);
            }
        }

        public class ResultAvailableAndConsumedAfterMethod
        {
            [Fact]
            public void ShouldCollectResultAvailableAfter()
            {
                IDictionary<string, object> meta = new Dictionary<string, object>
                {
                    {"fields",  new List<object>() },
                    {"result_available_after", 12345},
                    {"result_consumed_after", 67890}
                };

                var collector = NewSummaryCollector();;
                collector.CollectWithFields(meta);
                var summary = collector.Build();

                summary.ResultAvailableAfter.ToString().Should().Be("00:00:12.3450000");
                summary.ResultConsumedAfter.ToString().Should().Be("-00:00:00.0010000");
            }

            [Fact]
            public void ShouldCollectResultConsumedAfter()
            {
                IDictionary<string, object> meta = new Dictionary<string, object>
                {
                    {"result_available_after", 12345},
                    {"result_consumed_after", 67890}
                };

                var collector = NewSummaryCollector();;
                collector.Collect(meta);
                var summary = collector.Build();

                summary.ResultAvailableAfter.ToString().Should().Be("-00:00:00.0010000");
                summary.ResultConsumedAfter.ToString().Should().Be("00:01:07.8900000");
            }
        }
    }
}
