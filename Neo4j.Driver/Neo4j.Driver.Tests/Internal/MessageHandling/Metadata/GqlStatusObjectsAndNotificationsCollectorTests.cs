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

#pragma warning disable CS0618 // Type or member is obsolete
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
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.MessageHandling.Metadata;
using Neo4j.Driver.Internal.Result;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.MessageHandling.Metadata;

public class GqlStatusObjectsAndNotificationsCollectorTests
{
    [Fact]
    public void ShouldNotCollectAnythingIfMetadataIsNull()
    {
        var collector = new GqlStatusObjectsAndNotificationsCollector(true);
        collector.Collect(null);

        var collected = collector.Collected;
        collected.Should().BeNull();
    }

    [Fact]
    public void ShouldCreateNonNullObjectWhenMissingRelevantKeys()
    {
        var collector = new GqlStatusObjectsAndNotificationsCollector(true);
        collector.Collect(new Dictionary<string, object> { ["nope"] = true });

        var collected = collector.Collected;
        collected.Should().NotBeNull();
        collected.Notifications.Should().BeNull();
        collected.GqlStatusObjects.Should().BeNull();
    }

    [Fact]
    public void ShouldCollectEmptyNotifications()
    {
        var collector = new GqlStatusObjectsAndNotificationsCollector(true);
        collector.Collect(new Dictionary<string, object> { ["notifications"] = new List<object>() });

        var collected = collector.Collected;
        collected.Should().NotBeNull();
        collected.Notifications.Should().NotBeNull().And.BeEmpty();
        collected.GqlStatusObjects.Should().BeNull();
    }

    [Fact]
    public void ShouldCollectEmptyStatusesAndPolyfillNotifications()
    {
        var collector = new GqlStatusObjectsAndNotificationsCollector(true);
        collector.Collect(new Dictionary<string, object> { ["statuses"] = new List<object>() });

        var collected = collector.Collected;
        collected.Should().NotBeNull();
        collected.GqlStatusObjects.Should().NotBeNull().And.BeEmpty();
        collected.Notifications.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void ShouldCollectGqlStatuses()
    {
        var collector = new GqlStatusObjectsAndNotificationsCollector(true);
        collector.Collect(
            new Dictionary<string, object>
            {
                ["statuses"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["gql_status"] = "000000",
                        ["status_description"] = "it is a status",
                        ["description"] = "it is a vintage status",
                        ["diagnostic_record"] = new Dictionary<string, object>
                        {
                            ["_severity"] = "WARNING",
                            ["_classification"] = "PERFORMANCE",
                            ["_position"] = new Dictionary<string, object>
                            {
                                ["offset"] = 1L,
                                ["line"] = 2L,
                                ["column"] = 3L
                            }
                        },
                        ["title"] = "blah",
                        ["neo4j_code"] = "Neo.Transient"
                    }
                }
            });

        var collected = collector.Collected;

        collected.GqlStatusObjects.Should()
            .SatisfyRespectively(
                x =>
                {
                    var first = x.Should().BeOfType<GqlStatusObject>().Which;
                    var dict = new Dictionary<string, object>
                    {
                        ["CURRENT_SCHEMA"] = "/",
                        ["OPERATION"] = "",
                        ["OPERATION_CODE"] = "0",
                        ["_severity"] = "WARNING",
                        ["_classification"] = "PERFORMANCE",
                        ["_position"] = new Dictionary<string, object>
                        {
                            ["offset"] = 1L,
                            ["line"] = 2L,
                            ["column"] = 3L
                        }
                    };

                    first.GqlStatus.Should().Be("000000");
                    first.StatusDescription.Should().Be("it is a status");
                    first.Position.Should().Be(new InputPosition(1, 2, 3));
                    first.Severity.Should().Be(NotificationSeverity.Warning);
                    first.RawSeverity.Should().Be("WARNING");
                    first.Classification.Should().Be(NotificationClassification.Performance);
                    first.DiagnosticRecord.Should().BeEquivalentTo(dict);
                    first.RawClassification.Should().Be("PERFORMANCE");
                    first.RawDiagnosticRecord.Should().Be(dict.ToContentString());
                    first.IsNotification.Should().BeTrue();
                    first.Title.Should().Be("blah");
                });
    }

    [Fact]
    public void ShouldCollectMinimalGqlStatuses()
    {
        var collector = new GqlStatusObjectsAndNotificationsCollector(true);
        collector.Collect(
            new Dictionary<string, object>
            {
                ["statuses"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["gql_status"] = "030000",
                        ["status_description"] = "it is a status"
                    }
                }
            });

        var collected = collector.Collected;

        collected.GqlStatusObjects.Should()
            .SatisfyRespectively(
                x =>
                {
                    var first = x.Should().BeOfType<GqlStatusObject>().Which;
                    var dict = new Dictionary<string, object>
                    {
                        ["CURRENT_SCHEMA"] = "/",
                        ["OPERATION"] = "",
                        ["OPERATION_CODE"] = "0"
                    };

                    first.GqlStatus.Should().Be("030000");
                    first.StatusDescription.Should().Be("it is a status");
                    first.Position.Should().BeNull();
                    first.Severity.Should().Be(NotificationSeverity.Unknown);
                    first.RawSeverity.Should().BeNull();
                    first.Classification.Should().Be(NotificationClassification.Unknown);
                    first.DiagnosticRecord.Should().BeEquivalentTo(dict);
                    first.RawClassification.Should().BeNull();
                    first.IsNotification.Should().BeFalse();
                    first.Title.Should().BeNull();
                });
    }

    [Fact]
    public void ShouldMergeDiagnosticRecord()
    {
        var collector = new GqlStatusObjectsAndNotificationsCollector(true);
        collector.Collect(
            new Dictionary<string, object>
            {
                ["statuses"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["gql_status"] = "030000",
                        ["status_description"] = "it is a status",
                        ["diagnostic_record"] = new Dictionary<string, object>
                        {
                            ["Example"] = "blah-de-blah",
                            ["OPERATION"] = "OP!"
                        }
                    }
                }
            });

        var collected = collector.Collected;

        collected.GqlStatusObjects.Should()
            .SatisfyRespectively(
                x =>
                {
                    var first = x.Should().BeOfType<GqlStatusObject>().Which;
                    var dict = new Dictionary<string, object>
                    {
                        ["Example"] = "blah-de-blah",
                        ["CURRENT_SCHEMA"] = "/",
                        ["OPERATION"] = "OP!",
                        ["OPERATION_CODE"] = "0"
                    };

                    first.DiagnosticRecord.Should().BeEquivalentTo(dict);
                });
    }

    [Fact]
    public void ShouldPolyfilGqlStatusesIntoNotifications()
    {
        var collector = new GqlStatusObjectsAndNotificationsCollector(true);
        collector.Collect(
            new Dictionary<string, object>
            {
                ["statuses"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["gql_status"] = "000000",
                        ["status_description"] = "it is a status",
                        ["description"] = "it is a vintage status",
                        ["diagnostic_record"] = new Dictionary<string, object>
                        {
                            ["_severity"] = "WARNING",
                            ["_classification"] = "PERFORMANCE",
                            ["_position"] = new Dictionary<string, object>
                            {
                                ["offset"] = 1L,
                                ["line"] = 2L,
                                ["column"] = 3L
                            }
                        },
                        ["title"] = "blah",
                        ["neo4j_code"] = "Neo.Transient"
                    }
                }
            });

        var collected = collector.Collected;

        collected.Notifications.Should()
            .SatisfyRespectively(
                x =>
                {
                    var first = x.Should().BeOfType<Notification>().Which;
                    first.Code.Should().Be("Neo.Transient");
                    first.Title.Should().Be("blah");
                    first.Description.Should().Be("it is a vintage status");
                    first.Position.Should().Be(new InputPosition(1, 2, 3));
                    first.SeverityLevel.Should().Be(NotificationSeverity.Warning);
                    first.RawSeverityLevel.Should().Be("WARNING");
                    first.Category.Should().Be(NotificationCategory.Performance);
                    first.RawCategory.Should().Be("PERFORMANCE");
                });
    }

    [Fact]
    public void ShouldCollectNotifications()
    {
        var collector = new GqlStatusObjectsAndNotificationsCollector(true);
        collector.Collect(
            new Dictionary<string, object>
            {
                ["notifications"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["description"] = "it is a vintage status",
                        ["severity"] = "WARNING",
                        ["category"] = "PERFORMANCE",
                        ["position"] = new Dictionary<string, object>
                        {
                            ["offset"] = 1L,
                            ["line"] = 2L,
                            ["column"] = 3L
                        },
                        ["title"] = "blah",
                        ["code"] = "Neo.Transient"
                    }
                }
            });

        var collected = collector.Collected;

        collected.Notifications.Should()
            .SatisfyRespectively(
                x =>
                {
                    var first = x.Should().BeOfType<Notification>().Which;
                    first.Code.Should().Be("Neo.Transient");
                    first.Title.Should().Be("blah");
                    first.Description.Should().Be("it is a vintage status");
                    first.Position.Should().Be(new InputPosition(1, 2, 3));
                    first.SeverityLevel.Should().Be(NotificationSeverity.Warning);
                    first.RawSeverityLevel.Should().Be("WARNING");
                    first.Category.Should().Be(NotificationCategory.Performance);
                    first.RawCategory.Should().Be("PERFORMANCE");
                });
    }

    [Fact]
    public void ShouldCollectNotificationsWithoutCategory()
    {
        var collector = new GqlStatusObjectsAndNotificationsCollector(true);
        collector.Collect(
            new Dictionary<string, object>
            {
                ["notifications"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["description"] = "it is a vintage status",
                        ["severity"] = "WARNING",
                        ["position"] = new Dictionary<string, object>
                        {
                            ["offset"] = 1L,
                            ["line"] = 2L,
                            ["column"] = 3L
                        },
                        ["title"] = "blah",
                        ["code"] = "Neo.Transient"
                    }
                }
            });

        var collected = collector.Collected;

        collected.Notifications.Should()
            .SatisfyRespectively(
                x =>
                {
                    var first = x.Should().BeOfType<Notification>().Which;
                    first.Code.Should().Be("Neo.Transient");
                    first.Title.Should().Be("blah");
                    first.Description.Should().Be("it is a vintage status");
                    first.Position.Should().Be(new InputPosition(1, 2, 3));
                    first.SeverityLevel.Should().Be(NotificationSeverity.Warning);
                    first.RawSeverityLevel.Should().Be("WARNING");
                    first.Category.Should().Be(NotificationCategory.Unknown);
                    first.RawCategory.Should().BeNull();
                });
    }

    [Fact]
    public void ShouldCollectMinimalNotification()
    {
        var collector = new GqlStatusObjectsAndNotificationsCollector(true);
        collector.Collect(
            new Dictionary<string, object>
            {
                ["notifications"] = new List<object>
                {
                    new Dictionary<string, object>()
                }
            });

        var collected = collector.Collected;

        collected.Notifications.Should()
            .SatisfyRespectively(
                x =>
                {
                    var first = x.Should().BeOfType<Notification>().Which;
                    first.Code.Should().Be("");
                    first.Title.Should().Be("");
                    first.Description.Should().Be("");
                    first.Position.Should().BeNull();
                    first.SeverityLevel.Should().Be(NotificationSeverity.Unknown);
                    first.RawSeverityLevel.Should().Be("");
                    first.Category.Should().Be(NotificationCategory.Unknown);
                    first.RawCategory.Should().BeNull();
                });
    }

    [Fact]
    public void ShouldUpgradeMinimalNotificationToGqlStatus()
    {
        var collector = new GqlStatusObjectsAndNotificationsCollector(false);
        collector.Collect(
            new Dictionary<string, object>
            {
                ["notifications"] = new List<object>
                {
                    new Dictionary<string, object>()
                }
            });

        var collected = collector.Collected;

        collected.GqlStatusObjects.Should()
            .SatisfyRespectively(
                x =>
                {
                    var dict = new Dictionary<string, object>
                    {
                        ["CURRENT_SCHEMA"] = "/",
                        ["OPERATION"] = "",
                        ["OPERATION_CODE"] = "0"
                    };

                    var first = x.Should().BeOfType<GqlStatusObject>().Which;
                    first.GqlStatus.Should().Be("03N42");
                    first.Title.Should().BeNull();
                    first.StatusDescription.Should().Be("info: unknown notification");
                    first.Position.Should().BeNull();
                    first.Severity.Should().Be(NotificationSeverity.Unknown);
                    first.RawSeverity.Should().BeNull();
                    first.Classification.Should().Be(NotificationClassification.Unknown);
                    first.RawClassification.Should().BeNull();
                    first.DiagnosticRecord.Should().BeEquivalentTo(dict);
                });
    }

    [Fact]
    public void ShouldUpgradeNotificationToGqlStatus()
    {
        var collector = new GqlStatusObjectsAndNotificationsCollector(false);
        collector.Collect(
            new Dictionary<string, object>
            {
                ["notifications"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["description"] = "it is a vintage status",
                        ["severity"] = "WARNING",
                        ["position"] = new Dictionary<string, object>
                        {
                            ["offset"] = 1L,
                            ["line"] = 2L,
                            ["column"] = 3L
                        },
                        ["title"] = "blah",
                        ["code"] = "Neo.Transient"
                    }
                }
            });

        var collected = collector.Collected;

        collected.GqlStatusObjects.Should()
            .SatisfyRespectively(
                x =>
                {
                    var dict = new Dictionary<string, object>
                    {
                        ["_severity"] = "WARNING",
                        ["_position"] = new Dictionary<string, object>
                        {
                            ["offset"] = 1L,
                            ["line"] = 2L,
                            ["column"] = 3L
                        },
                        ["CURRENT_SCHEMA"] = "/",
                        ["OPERATION"] = "",
                        ["OPERATION_CODE"] = "0"
                    };

                    var first = x.Should().BeOfType<GqlStatusObject>().Which;
                    first.GqlStatus.Should().Be("01N42");
                    first.Title.Should().Be("blah");
                    first.StatusDescription.Should().Be("it is a vintage status");
                    first.Position.Should().Be(new InputPosition(1, 2, 3));
                    first.Severity.Should().Be(NotificationSeverity.Warning);
                    first.RawSeverity.Should().Be("WARNING");
                    first.Classification.Should().Be(NotificationClassification.Unknown);
                    first.RawClassification.Should().BeNull();
                    first.DiagnosticRecord.Should().BeEquivalentTo(dict);
                });
    }

    [Fact]
    public void ShouldNotPolyfilWhenProvidedGqlStatus()
    {
        var collector = new GqlStatusObjectsAndNotificationsCollector(true);
        collector.Collect(
            new Dictionary<string, object>
            {
                ["notifications"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["description"] = "it is a vintage status",
                        ["severity"] = "WARNING",
                        ["position"] = new Dictionary<string, object>
                        {
                            ["offset"] = 1L,
                            ["line"] = 2L,
                            ["column"] = 3L
                        },
                        ["title"] = "blah",
                        ["code"] = "Neo.Transient"
                    }
                },
                ["statuses"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["gql_status"] = "000000",
                        ["status_description"] = "it is a status",
                        ["description"] = "it is a vintage status",
                        ["diagnostic_record"] = new Dictionary<string, object>
                        {
                            ["_severity"] = "INFORMATION",
                            ["_classification"] = "HINT",
                            ["_position"] = new Dictionary<string, object>
                            {
                                ["offset"] = 4L,
                                ["line"] = 5L,
                                ["column"] = 6L
                            }
                        },
                        ["title"] = "blah",
                        ["neo4j_code"] = "Neo.Transient"
                    }
                }
            });

        var collected = collector.Collected;

        collected.Notifications.Should()
            .HaveCount(1)
            .And
            .SatisfyRespectively(
                x =>
                {
                    var first = x.Should().BeOfType<Notification>().Which;
                    first.Code.Should().Be("Neo.Transient");
                    first.Title.Should().Be("blah");
                    first.Description.Should().Be("it is a vintage status");
                    first.Position.Should().Be(new InputPosition(1, 2, 3));
                    first.SeverityLevel.Should().Be(NotificationSeverity.Warning);
                    first.RawSeverityLevel.Should().Be("WARNING");
                    first.Category.Should().Be(NotificationCategory.Unknown);
                    first.RawCategory.Should().Be(null);
                });

        collected.GqlStatusObjects.Should()
            .HaveCount(1)
            .And
            .SatisfyRespectively(
                x =>
                {
                    var first = x.Should().BeOfType<GqlStatusObject>().Which;
                    var dict = new Dictionary<string, object>
                    {
                        ["CURRENT_SCHEMA"] = "/",
                        ["OPERATION"] = "",
                        ["OPERATION_CODE"] = "0",
                        ["_severity"] = "INFORMATION",
                        ["_classification"] = "HINT",
                        ["_position"] = new Dictionary<string, object>
                        {
                            ["offset"] = 4L,
                            ["line"] = 5L,
                            ["column"] = 6L
                        }
                    };

                    first.GqlStatus.Should().Be("000000");
                    first.StatusDescription.Should().Be("it is a status");
                    first.Position.Should().Be(new InputPosition(4, 5, 6));
                    first.Severity.Should().Be(NotificationSeverity.Information);
                    first.RawSeverity.Should().Be("INFORMATION");
                    first.Classification.Should().Be(NotificationClassification.Hint);
                    first.DiagnosticRecord.Should().BeEquivalentTo(dict);
                    first.RawClassification.Should().Be("HINT");
                    first.RawDiagnosticRecord.Should().Be(dict.ToContentString());
                    first.IsNotification.Should().BeTrue();
                    first.Title.Should().Be("blah");
                });
    }
}
