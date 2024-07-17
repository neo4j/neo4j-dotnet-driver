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
    public void ShouldNotCollectAnythingIfMetadataDoesNotContainStatuses()
    {
        var collector = new GqlStatusObjectsAndNotificationsCollector(true);
        collector.Collect(new Dictionary<string, object>());

        var collected = collector.Collected;
        collected.Should().NotBeNull();
        collected.Notifications.Should().BeEmpty();
        collected.GqlStatusObjects.Should().BeEmpty();
    }

    [Fact]
    public void ShouldCollectGqlStatusObjects()
    {
        var metadata = new Dictionary<string, object>
        {
            {
                "statuses", new List<object>
                {
                    new Dictionary<string, object>
                    {
                        { "gql_status", "OK" },
                        { "gql_message", "Everything is fine" },
                        { "status_description", "Everything is fine" },
                        { "neo4j_code", "neo4j_code" },
                        { "title", "title" },
                        {
                            "diagnostic_record", new Dictionary<string, object>()
                            {
                                { "_severity", "WARNING" },
                                { "_classification", "SECURITY" },
                                {
                                    "_position", new Dictionary<string, object>()
                                    {
                                        { "offset", 42 },
                                        { "line", 4242 },
                                        { "column", 424242 }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        var collector = new GqlStatusObjectsAndNotificationsCollector(false);
        collector.Collect(metadata);

        collector.Collected.Should().NotBeNull();
        collector.Collected.GqlStatusObjects.Should().NotBeNull();
        collector.Collected.GqlStatusObjects.Should().HaveCount(1);
        collector.Collected.GqlStatusObjects[0].GqlStatus.Should().Be("OK");
        collector.Collected.GqlStatusObjects[0].StatusDescription.Should().Be("Everything is fine");
    }

    [Fact]
    public void ShouldExtractGqlStatusObjects()
    {
        var gqlStatusObject1 = new Dictionary<string, object>
        {
            { "gql_status", "gql_status" },
            { "status_description", "status_description" },
            { "neo4j_code", "neo4j_code" },
            { "title", "title" },
            {
                "diagnostic_record", new Dictionary<string, object>
                {
                    { "_severity", "WARNING" },
                    { "_classification", "SECURITY" },
                    {
                        "_position", new Dictionary<string, object>
                        {
                            { "offset", 42 },
                            { "line", 4242 },
                            { "column", 424242 }
                        }
                    }
                }
            }
        };

        var gqlStatusObject2 = new Dictionary<string, object>
        {
            { "gql_status", "gql_status" },
            { "status_description", "status_description" },
            {
                "diagnostic_record", new Dictionary<string, object>
                {
                    { "_severity", "WARNING" },
                    { "_classification", "SECURITY" }
                }
            }
        };

        var gqlStatusObjects = new List<object> { gqlStatusObject1, gqlStatusObject2 };
        var metadata = new Dictionary<string, object> { { "statuses", gqlStatusObjects } };

        var extractor = new GqlStatusObjectsAndNotificationsCollector(false);
        extractor.Collect(metadata);
        var collected = extractor.Collected;

        collected.GqlStatusObjects.Should().HaveCount(2);
        var firstGqlStatusObject = (INotification)collected.GqlStatusObjects[0];
        var secondGqlStatusObject = collected.GqlStatusObjects[1];

        firstGqlStatusObject.GqlStatus.Should().Be("gql_status");
        firstGqlStatusObject.StatusDescription.Should().Be("status_description");
        firstGqlStatusObject.Description.Should().Be("status_description");
        firstGqlStatusObject.Code.Should().Be("neo4j_code");
        firstGqlStatusObject.Title.Should().Be("title");
        firstGqlStatusObject.Severity.Should().Be("WARNING");
        firstGqlStatusObject.SeverityLevel.Should().Be(NotificationSeverity.Warning);
        firstGqlStatusObject.RawSeverityLevel.Should().Be("WARNING");
        firstGqlStatusObject.Category.Should().Be(NotificationCategory.Security);
        firstGqlStatusObject.RawCategory.Should().Be("SECURITY");
        firstGqlStatusObject.Position.Should().BeEquivalentTo(new InputPosition(42, 4242, 424242));
        firstGqlStatusObject.DiagnosticRecord
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object>
                {
                    { "OPERATION", "" },
                    { "OPERATION_CODE", "0" },
                    { "CURRENT_SCHEMA", "/" },
                    { "_severity", "WARNING" },
                    { "_classification", "SECURITY" },
                    {
                        "_position", new Dictionary<string, object>
                        {
                            { "offset", 42 },
                            { "line", 4242 },
                            { "column", 424242 }
                        }
                    }
                });

        (secondGqlStatusObject is Notification).Should().BeFalse();
        secondGqlStatusObject.GqlStatus.Should().Be("gql_status");
        secondGqlStatusObject.StatusDescription.Should().Be("status_description");
        secondGqlStatusObject.DiagnosticRecord
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object>
                {
                    { "OPERATION", "" },
                    { "OPERATION_CODE", "0" },
                    { "CURRENT_SCHEMA", "/" },
                    { "_severity", "WARNING" },
                    { "_classification", "SECURITY" },
                    {
                        "_position", new Dictionary<string, object>
                        {
                            { "offset", 42 },
                            { "line", 4242 },
                            { "column", 424242 }
                        }
                    }
                });

        collected.Notifications.Should().HaveCount(1);
        collected.Notifications[0].Should().Be(firstGqlStatusObject);
    }
}
