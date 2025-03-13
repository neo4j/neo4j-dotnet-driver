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

public class GqlStatusObjectsAndNotificationsTests
{
    [Fact]
    public void FinalizeStatusObjectsShouldReturnAnEmptyList()
    {
        var objs = new GqlStatusObjectsAndNotifications(null, null, true);

        objs.FinalizeStatusObjects(new CursorMetadata()).Should().NotBeNull().And.Subject.Should().BeEmpty();
    }

    [Fact]
    public void FinalizeNotificationsShouldReturnNull()
    {
        var objs = new GqlStatusObjectsAndNotifications(null, null, true);

        objs.FinalizeNotifications(new CursorMetadata()).Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(FinalizeStatusObjectsShouldPolyfilData))]
    public void FinalizeStatusObjectsShouldPolyfil(bool hadRecords, bool hadKeys, IGqlStatusObject exp)
    {
        var objs = new GqlStatusObjectsAndNotifications(null, null, false);

        objs.FinalizeStatusObjects(new CursorMetadata(hadRecords, hadKeys))
            .Should()
            .HaveCount(1)
            .And.Subject.Should()
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

                    first.GqlStatus.Should().Be(exp.GqlStatus);
                    first.StatusDescription.Should().Be(exp.StatusDescription);
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

    public static IEnumerable<object[]> FinalizeStatusObjectsShouldPolyfilData()
    {
        yield return [true, true, GqlStatusObject.Success];
        yield return [false, false, GqlStatusObject.OmittedResult];
        yield return [false, true, GqlStatusObject.NoData];
    }

    [Fact]
    public void ShouldOrderByCodeWhenPolyfilling()
    {
        var dict = new Dictionary<string, object>
        {
            ["CURRENT_SCHEMA"] = "/",
            ["OPERATION"] = "",
            ["OPERATION_CODE"] = "0"
        };

        var statuses = new List<IGqlStatusObject>
        {
            new GqlStatusObject("04-", "", null, null, null, dict, null, false),
            new GqlStatusObject("01-", "", null, null, null, dict, null, false),
            new GqlStatusObject("03-", "", null, null, null, dict, null, false),
            new GqlStatusObject("02-", "", null, null, null, dict, null, false),
            new GqlStatusObject("1", "", null, null, null, dict, null, false)
        };

        var objs = new GqlStatusObjectsAndNotifications(null, statuses, false);

        objs.FinalizeStatusObjects(new CursorMetadata(true, true))
            .Should()
            .HaveCount(6)
            .And
            .SatisfyRespectively(
                x => x.GqlStatus.Should().Be("02-"),
                x => x.GqlStatus.Should().Be("01-"),
                x => x.GqlStatus.Should().Be("00000"),
                x => x.GqlStatus.Should().Be("03-"),
                x => x.GqlStatus.Should().Be("04-"),
                x => x.GqlStatus.Should().Be("1"));
    }
}
