// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
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
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.MessageHandling.Metadata;
using Neo4j.Driver.Internal.Result;
using Xunit;
using Record = Xunit.Record;

namespace Neo4j.Driver.Tests.Internal.MessageHandling.Metadata;

public class PlanCollectorTests
{
    public const string Key = PlanCollector.PlanKey;

    internal static KeyValuePair<string, object> TestMetadata =>
        new(
            Key,
            new Dictionary<string, object>
            {
                { "operatorType", "opType" },
                { "args", new Dictionary<string, object> { { "a", 1L } } },
                {
                    "identifiers", new List<object>
                    {
                        "a", "b", "c"
                    }
                }
            });

    internal static IPlan TestMetadataCollected => new Plan(
        "opType",
        new Dictionary<string, object> { { "a", 1L } },
        new List<string> { "a", "b", "c" },
        new List<IPlan>());

    [Fact]
    public void ShouldNotCollectIfMetadataIsNull()
    {
        var collector = new PlanCollector();

        collector.Collect(null);

        collector.Collected.Should().BeNull();
    }

    [Fact]
    public void ShouldNotCollectIfNoValueIsGiven()
    {
        var collector = new PlanCollector();

        collector.Collect(new Dictionary<string, object>());

        collector.Collected.Should().BeNull();
    }

    [Fact]
    public void ShouldNotCollectIfValueIsNull()
    {
        var collector = new PlanCollector();

        collector.Collect(new Dictionary<string, object> { { Key, null } });

        collector.Collected.Should().BeNull();
    }

    [Fact]
    public void ShouldNotCollectIfValueIsEmpty()
    {
        var collector = new PlanCollector();

        collector.Collect(new Dictionary<string, object> { { Key, new Dictionary<string, object>() } });

        collector.Collected.Should().BeNull();
    }

    [Fact]
    public void ShouldThrowIfValueIsOfWrongType()
    {
        var metadata = new Dictionary<string, object> { { Key, true } };
        var collector = new PlanCollector();

        var ex = Record.Exception(() => collector.Collect(metadata));

        ex.Should()
            .BeOfType<ProtocolException>()
            .Which
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
                    { "args", new Dictionary<string, object>() }
                }
            }
        };

        var collector = new PlanCollector();

        var ex = Record.Exception(() => collector.Collect(metadata));

        ex.Should()
            .BeOfType<ProtocolException>()
            .Which
            .Message.Should()
            .Be("Expected key 'operatorType' to be present in the dictionary, but could not find.");
    }

    [Fact]
    public void ShouldCollectWithDefaultValues()
    {
        var metadata = new Dictionary<string, object>
        {
            {
                Key, new Dictionary<string, object>
                {
                    { "operatorType", "opType" }
                }
            }
        };

        var collector = new PlanCollector();

        collector.Collect(metadata);

        collector.Collected.Should()
            .BeEquivalentTo(
                new Plan(
                    "opType",
                    new Dictionary<string, object>(),
                    new List<string>(),
                    new List<IPlan>()));
    }

    [Fact]
    public void ShouldCollectWithoutChildPlans()
    {
        var metadata = new Dictionary<string, object>
        {
            {
                Key, new Dictionary<string, object>
                {
                    { "operatorType", "opType" },
                    { "args", new Dictionary<string, object> { { "a", 1L } } },
                    {
                        "identifiers", new List<object>
                        {
                            "a", "b", "c"
                        }
                    }
                }
            }
        };

        var collector = new PlanCollector();

        collector.Collect(metadata);

        collector.Collected.Should()
            .BeEquivalentTo(
                new Plan(
                    "opType",
                    new Dictionary<string, object> { { "a", 1L } },
                    new List<string> { "a", "b", "c" },
                    new List<IPlan>()));
    }

    [Fact]
    public void ShouldCollectWithChildPlans()
    {
        var metadata = new Dictionary<string, object>
        {
            {
                Key, new Dictionary<string, object>
                {
                    { "operatorType", "opType" },
                    { "args", new Dictionary<string, object> { { "a", 1L } } },
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
                                { "operatorType", "childOpType" },
                                { "args", new Dictionary<string, object> { { "b", 2L } } },
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

        var collector = new PlanCollector();

        collector.Collect(metadata);

        collector.Collected.Should()
            .BeEquivalentTo(
                new Plan(
                    "opType",
                    new Dictionary<string, object> { { "a", 1L } },
                    new List<string> { "a", "b", "c" },
                    new List<IPlan>
                    {
                        new Plan(
                            "childOpType",
                            new Dictionary<string, object> { { "b", 2L } },
                            new List<string> { "d", "e" },
                            new List<IPlan>())
                    }));
    }

    [Fact]
    public void ShouldCollectWithNestedChildPlans()
    {
        var metadata = new Dictionary<string, object>
        {
            {
                Key, new Dictionary<string, object>
                {
                    { "operatorType", "opType" },
                    { "args", new Dictionary<string, object> { { "a", 1L } } },
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
                                { "operatorType", "childOpType" },
                                { "args", new Dictionary<string, object> { { "b", 2L } } },
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
                                            { "operatorType", "childChildOpType" },
                                            { "args", new Dictionary<string, object> { { "c", 3L } } },
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

        var collector = new PlanCollector();

        collector.Collect(metadata);

        collector.Collected.Should()
            .BeEquivalentTo(
                new Plan(
                    "opType",
                    new Dictionary<string, object> { { "a", 1L } },
                    new List<string> { "a", "b", "c" },
                    new List<IPlan>
                    {
                        new Plan(
                            "childOpType",
                            new Dictionary<string, object> { { "b", 2L } },
                            new List<string> { "d", "e" },
                            new List<IPlan>
                            {
                                new Plan(
                                    "childChildOpType",
                                    new Dictionary<string, object> { { "c", 3L } },
                                    new List<string> { "f" },
                                    new List<IPlan>())
                            })
                    }));
    }

    [Fact]
    public void ShouldReturnSameCollected()
    {
        var metadata = new Dictionary<string, object>
        {
            {
                Key, new Dictionary<string, object>
                {
                    { "operatorType", "opType" }
                }
            }
        };

        var collector = new PlanCollector();

        collector.Collect(metadata);

        ((IMetadataCollector)collector).Collected.Should().BeSameAs(collector.Collected);
    }
}
