// Copyright (c) 2002-2019 "Neo4j,"
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

using System.Collections.Generic;
using FluentAssertions;
using Neo4j.Driver.Internal.Result;
using Xunit;
using Record = Xunit.Record;

namespace Neo4j.Driver.Internal.MessageHandling.Metadata
{
    public class ProfiledPlanCollectorTests
    {
        public const string Key = ProfiledPlanCollector.ProfiledPlanKey;

        [Fact]
        public void ShouldNotCollectIfMetadataIsNull()
        {
            var collector = new ProfiledPlanCollector();

            collector.Collect(null);

            collector.Collected.Should().BeNull();
        }

        [Fact]
        public void ShouldNotCollectIfNoValueIsGiven()
        {
            var collector = new ProfiledPlanCollector();

            collector.Collect(new Dictionary<string, object>());

            collector.Collected.Should().BeNull();
        }

        [Fact]
        public void ShouldNotCollectIfValueIsNull()
        {
            var collector = new ProfiledPlanCollector();

            collector.Collect(new Dictionary<string, object> {{Key, null}});

            collector.Collected.Should().BeNull();
        }

        [Fact]
        public void ShouldNotCollectIfValueIsEmpty()
        {
            var collector = new ProfiledPlanCollector();

            collector.Collect(new Dictionary<string, object> {{Key, new Dictionary<string, object>()}});

            collector.Collected.Should().BeNull();
        }


        [Fact]
        public void ShouldThrowIfValueIsOfWrongType()
        {
            var metadata = new Dictionary<string, object> {{Key, true}};
            var collector = new ProfiledPlanCollector();

            var ex = Record.Exception(() => collector.Collect(metadata));

            ex.Should().BeOfType<ProtocolException>().Which
                .Message.Should()
                .Contain($"Expected '{Key}' metadata to be of type 'IDictionary<String,Object>', but got 'Boolean'.");
        }

        [Fact]
        public void ShouldThrowIfOperatorTypeIsMissing()
        {
            var metadata = new Dictionary<string, object>
            {
                {
                    Key, new Dictionary<string, object>
                    {
                        {"args", new Dictionary<string, object>()}
                    }
                }
            };
            var collector = new ProfiledPlanCollector();

            var ex = Record.Exception(() => collector.Collect(metadata));

            ex.Should().BeOfType<ProtocolException>().Which
                .Message.Should()
                .Be("Expected key 'operatorType' to be present in the dictionary, but could not find.");
        }

        [Fact]
        public void ShouldThrowIfDbHitsIsMissing()
        {
            var metadata = new Dictionary<string, object>
            {
                {
                    Key, new Dictionary<string, object>
                    {
                        {"operatorType", "opType"}
                    }
                }
            };
            var collector = new ProfiledPlanCollector();

            var ex = Record.Exception(() => collector.Collect(metadata));

            ex.Should().BeOfType<ProtocolException>().Which
                .Message.Should()
                .Be("Expected key 'dbHits' to be present in the dictionary, but could not find.");
        }

        [Fact]
        public void ShouldThrowIfRowsIsMissing()
        {
            var metadata = new Dictionary<string, object>
            {
                {
                    Key, new Dictionary<string, object>
                    {
                        {"operatorType", "opType"},
                        {"dbHits", 5L}
                    }
                }
            };
            var collector = new ProfiledPlanCollector();

            var ex = Record.Exception(() => collector.Collect(metadata));

            ex.Should().BeOfType<ProtocolException>().Which
                .Message.Should()
                .Be("Expected key 'rows' to be present in the dictionary, but could not find.");
        }

        [Fact]
        public void ShouldCollectWithDefaultValues()
        {
            var metadata = new Dictionary<string, object>
            {
                {
                    Key, new Dictionary<string, object>
                    {
                        {"operatorType", "opType"},
                        {"dbHits", 5L},
                        {"rows", 10L}
                    }
                }
            };
            var collector = new ProfiledPlanCollector();

            collector.Collect(metadata);

            collector.Collected.ShouldBeEquivalentTo(new ProfiledPlan("opType", new Dictionary<string, object>(),
                new List<string>(), new List<IProfiledPlan>(), 5, 10));
        }

        [Fact]
        public void ShouldCollectWithoutChildPlans()
        {
            var metadata = new Dictionary<string, object>
            {
                {
                    Key, new Dictionary<string, object>
                    {
                        {"operatorType", "opType"},
                        {"dbHits", 5L},
                        {"rows", 10L},
                        {"args", new Dictionary<string, object> {{"a", 1L}}},
                        {
                            "identifiers", new List<object>
                            {
                                "a", "b", "c"
                            }
                        }
                    }
                }
            };
            var collector = new ProfiledPlanCollector();

            collector.Collect(metadata);

            collector.Collected.ShouldBeEquivalentTo(new ProfiledPlan("opType",
                new Dictionary<string, object> {{"a", 1L}},
                new List<string> {"a", "b", "c"}, new List<IProfiledPlan>(), 5, 10));
        }

        [Fact]
        public void ShouldCollectWithChildPlans()
        {
            var metadata = new Dictionary<string, object>
            {
                {
                    Key, new Dictionary<string, object>
                    {
                        {"operatorType", "opType"},
                        {"dbHits", 5L},
                        {"rows", 10L},
                        {"args", new Dictionary<string, object> {{"a", 1L}}},
                        {
                            "identifiers", new List<object>
                            {
                                "a", "b", "c"
                            }
                        },
                        {
                            "children", new List<object>
                            {
                                new Dictionary<string, object>
                                {
                                    {"operatorType", "childOpType"},
                                    {"dbHits", 15L},
                                    {"rows", 20L},
                                    {"args", new Dictionary<string, object> {{"b", 2L}}},
                                    {
                                        "identifiers", new List<object>
                                        {
                                            "d", "e"
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };
            var collector = new ProfiledPlanCollector();

            collector.Collect(metadata);

            collector.Collected.ShouldBeEquivalentTo(new ProfiledPlan("opType",
                new Dictionary<string, object> {{"a", 1L}},
                new List<string> {"a", "b", "c"},
                new List<IProfiledPlan>
                {
                    new ProfiledPlan("childOpType", new Dictionary<string, object> {{"b", 2L}},
                        new List<string> {"d", "e"},
                        new List<IProfiledPlan>(), 15, 20)
                }, 5, 10));
        }

        [Fact]
        public void ShouldCollectWithNestedChildPlans()
        {
            var metadata = new Dictionary<string, object>
            {
                {
                    Key, new Dictionary<string, object>
                    {
                        {"operatorType", "opType"},
                        {"dbHits", 5L},
                        {"rows", 10L},
                        {"args", new Dictionary<string, object> {{"a", 1L}}},
                        {
                            "identifiers", new List<object>
                            {
                                "a", "b", "c"
                            }
                        },
                        {
                            "children", new List<object>
                            {
                                new Dictionary<string, object>
                                {
                                    {"operatorType", "childOpType"},
                                    {"dbHits", 15L},
                                    {"rows", 20L},
                                    {"args", new Dictionary<string, object> {{"b", 2L}}},
                                    {
                                        "identifiers", new List<object>
                                        {
                                            "d", "e"
                                        }
                                    },
                                    {
                                        "children", new List<object>
                                        {
                                            new Dictionary<string, object>
                                            {
                                                {"operatorType", "childChildOpType"},
                                                {"dbHits", 25L},
                                                {"rows", 30L},
                                                {"args", new Dictionary<string, object> {{"c", 3L}}},
                                                {
                                                    "identifiers", new List<object>
                                                    {
                                                        "f"
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };
            var collector = new ProfiledPlanCollector();

            collector.Collect(metadata);

            collector.Collected.ShouldBeEquivalentTo(new ProfiledPlan("opType",
                new Dictionary<string, object> {{"a", 1L}},
                new List<string> {"a", "b", "c"},
                new List<IProfiledPlan>
                {
                    new ProfiledPlan("childOpType", new Dictionary<string, object> {{"b", 2L}},
                        new List<string> {"d", "e"},
                        new List<IProfiledPlan>
                        {
                            new ProfiledPlan("childChildOpType", new Dictionary<string, object> {{"c", 3L}},
                                new List<string> {"f"},
                                new List<IProfiledPlan>(), 25, 30)
                        }, 15, 20)
                }, 5, 10));
        }

        [Fact]
        public void ShouldReturnSameCollected()
        {
            var metadata = new Dictionary<string, object>
            {
                {
                    Key, new Dictionary<string, object>
                    {
                        {"operatorType", "opType"},
                        {"dbHits", 5L},
                        {"rows", 10L}
                    }
                }
            };
            var collector = new ProfiledPlanCollector();

            collector.Collect(metadata);

            ((IMetadataCollector) collector).Collected.Should().BeSameAs(collector.Collected);
        }

        internal static KeyValuePair<string, object> TestMetadata =>
            new KeyValuePair<string, object>(Key, new Dictionary<string, object>
            {
                {"operatorType", "opType"},
                {"dbHits", 5L},
                {"rows", 10L},
                {"args", new Dictionary<string, object> {{"a", 1L}}},
                {
                    "identifiers", new List<object>
                    {
                        "a", "b", "c"
                    }
                }
            });

        internal static IProfiledPlan TestMetadataCollected => new ProfiledPlan("opType",
            new Dictionary<string, object> {{"a", 1L}},
            new List<string> {"a", "b", "c"}, new List<IProfiledPlan>(), 5, 10);
    }
}