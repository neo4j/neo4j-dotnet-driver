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

public class NotificationsCollectorTests
{
    public const string Key = NotificationsCollector.NotificationsKey;

    internal static KeyValuePair<string, object> TestMetadata =>
        new(
            Key,
            new List<object>
            {
                new Dictionary<string, object>
                {
                    { "code", "code1" },
                    { "title", "title1" },
                    { "description", "description1" },
                    { "severity", "severity1" },
                    {
                        "position", new Dictionary<string, object>
                        {
                            { "offset", 1L },
                            { "line", 2L },
                            { "column", 3L }
                        }
                    }
                }
            });

    internal static IList<INotification> TestMetadataCollected => new List<INotification>
    {
        new Notification(
            "gqlStatus",
            "statusDescription",
            null,
            "code1",
            "title1",
            "description1",
            new InputPosition(1, 2, 3),
            "severity1",
            "category1")
    };

    [Fact]
    public void ShouldNotCollectIfMetadataIsNull()
    {
        var collector = new NotificationsCollector();

        collector.Collect(null);

        collector.Collected.Should().BeNull();
    }

    [Fact]
    public void ShouldNotCollectIfNoValueIsGiven()
    {
        var collector = new NotificationsCollector();

        collector.Collect(new Dictionary<string, object>());

        collector.Collected.Should().BeNull();
    }

    [Fact]
    public void ShouldNotCollectIfValueIsNull()
    {
        var collector = new NotificationsCollector();

        collector.Collect(new Dictionary<string, object> { { Key, null } });

        collector.Collected.Should().BeNull();
    }

    [Fact]
    public void ShouldThrowIfValueIsOfWrongType()
    {
        var metadata = new Dictionary<string, object> { { Key, 3 } };
        var collector = new NotificationsCollector();

        var ex = Record.Exception(() => collector.Collect(metadata));

        ex.Should()
            .BeOfType<ProtocolException>()
            .Which
            .Message.Should()
            .Contain($"Expected '{Key}' metadata to be of type 'List<Object>', but got 'Int32'.");
    }

    [Fact]
    public void ShouldCollect()
    {
        var metadata = new Dictionary<string, object>
        {
            {
                Key, new List<object>
                {
                    new Dictionary<string, object>
                    {
                        { "code", "code1" },
                        { "title", "title1" },
                        { "description", "description1" },
                        { "severity", "severity1" },
                        {
                            "position", new Dictionary<string, object>
                            {
                                { "offset", 1L },
                                { "line", 2L },
                                { "column", 3L }
                            }
                        },
                        { "category", "category1" }
                    }
                }
            }
        };

        var collector = new NotificationsCollector();

        collector.Collect(metadata);

        collector.Collected.Should()
            .BeEquivalentTo(
                new Notification(
                    "03N42",
                    "description1",
                    new Dictionary<string, object>
                    {
                        ["OPERATION"] = "",
                        ["OPERATION_CODE"] = "0",
                        ["CURRENT_SCHEMA"] = "/"
                    },
                    "code1",
                    "title1",
                    "description1",
                    new InputPosition(1, 2, 3),
                    "severity1",
                    "category1"));
    }

    [Fact]
    public void ShouldCollectNullCategory()
    {
        var metadata = new Dictionary<string, object>
        {
            {
                Key, new List<object>
                {
                    new Dictionary<string, object>
                    {
                        { "code", "code1" },
                        { "title", "title1" },
                        { "description", "description1" },
                        { "severity", "severity1" },
                        {
                            "position", new Dictionary<string, object>
                            {
                                { "offset", 1L },
                                { "line", 2L },
                                { "column", 3L }
                            }
                        },
                    }
                }
            }
        };

        var collector = new NotificationsCollector();

        collector.Collect(metadata);

        collector.Collected.Should()
            .BeEquivalentTo(
                new Notification(
                    "03N42",
                    "description1",
                    new Dictionary<string, object>
                    {
                        ["OPERATION"] = "",
                        ["OPERATION_CODE"] = "0",
                        ["CURRENT_SCHEMA"] = "/"
                    },
                    "code1",
                    "title1",
                    "description1",
                    new InputPosition(1, 2, 3),
                    "severity1",
                    null));
    }

    [Fact]
    public void ShouldCollectList()
    {
        var metadata = new Dictionary<string, object>
        {
            {
                Key, new List<object>
                {
                    new Dictionary<string, object>
                    {
                        { "code", "code1" },
                        { "title", "title1" },
                        { "description", "description1" },
                        { "severity", "severity1" },
                        {
                            "position", new Dictionary<string, object>
                            {
                                { "offset", 1L },
                                { "line", 2L },
                                { "column", 3L }
                            }
                        }
                    },
                    new Dictionary<string, object>
                    {
                        { "code", "code2" },
                        { "title", "title2" },
                        { "description", "description2" },
                        { "severity", "severity2" },
                        {
                            "position", new Dictionary<string, object>
                            {
                                { "offset", 4L },
                                { "line", 5L }
                            }
                        }
                    }
                }
            }
        };

        var collector = new NotificationsCollector();

        collector.Collect(metadata);

        collector.Collected.Should()
            .BeEquivalentTo(
                new Notification(
                    "03N42",
                    "description1",
                    new Dictionary<string, object>
                    {
                        ["OPERATION"] = "",
                        ["OPERATION_CODE"] = "0",
                        ["CURRENT_SCHEMA"] = "/"
                    },
                    "code1",
                    "title1",
                    "description1",
                    new InputPosition(1, 2, 3),
                    "severity1",
                    null),
                new Notification(
                    "03N42",
                    "description2",
                    new Dictionary<string, object>
                    {
                        ["OPERATION"] = "",
                        ["OPERATION_CODE"] = "0",
                        ["CURRENT_SCHEMA"] = "/"
                    },
                    "code2",
                    "title2",
                    "description2",
                    new InputPosition(4, 5, 0),
                    "severity2",
                    null));
}

[Fact]
public void ShouldReturnSameCollected()
{
    var metadata = new Dictionary<string, object>
    {
        {
            Key, new List<object>
            {
                new Dictionary<string, object>
                {
                    { "code", "code1" },
                    { "title", "title1" },
                    { "description", "description1" },
                    { "severity", "severity1" },
                    {
                        "position", new Dictionary<string, object>
                        {
                            { "offset", 1L },
                            { "line", 2L },
                            { "column", 3L }
                        }
                    }
                }
            }
        }
    };

    var collector = new NotificationsCollector();

    collector.Collect(metadata);

    ((IMetadataCollector)collector).Collected.Should().BeSameAs(collector.Collected);
}

}
