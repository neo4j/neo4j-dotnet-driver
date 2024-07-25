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
using FluentAssertions.Equivalency;
using Moq;
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
}
