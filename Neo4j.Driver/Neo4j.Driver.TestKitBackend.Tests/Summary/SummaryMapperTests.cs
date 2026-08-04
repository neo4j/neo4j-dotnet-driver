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

#pragma warning disable CS0618 // Notifications is obsolete but still part of the wire contract.

using System.Text.Json;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.TestKitBackend.Cypher;
using Neo4j.Driver.TestKitBackend.Summary;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Summary;

public class SummaryMapperTests
{
    private readonly Mock<INativeToCypherMapper> _cypherMapperMock = new();
    private readonly SummaryMapper _mapper;

    public SummaryMapperTests()
    {
        _mapper = new SummaryMapper(_cypherMapperMock.Object);
    }

    private static Mock<IResultSummary> CreateSummaryMock()
    {
        var counters = new Counters(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, true, true);

        var serverInfo = new ServerInfo(new Uri("bolt://localhost:7687"));
        serverInfo.Update(new BoltProtocolVersion(5, 4), "Neo4j/5.20.0");

        var summaryMock = new Mock<IResultSummary>();
        summaryMock.Setup(s => s.Query).Returns(new Query("RETURN 1", new Dictionary<string, object>()));
        summaryMock.Setup(s => s.QueryType).Returns(QueryType.ReadOnly);
        summaryMock.Setup(s => s.HasPlan).Returns(false);
        summaryMock.Setup(s => s.HasProfile).Returns(false);
        summaryMock.Setup(s => s.Notifications).Returns(new List<INotification>());
        summaryMock.Setup(s => s.GqlStatusObjects).Returns(new List<IGqlStatusObject>());
        summaryMock.Setup(s => s.Database).Returns(new DatabaseInfo("neo4j"));
        summaryMock.Setup(s => s.Server).Returns(serverInfo);
        summaryMock.Setup(s => s.Counters).Returns(counters);
        summaryMock.Setup(s => s.ResultAvailableAfter).Returns(TimeSpan.FromMilliseconds(5));
        summaryMock.Setup(s => s.ResultConsumedAfter).Returns(TimeSpan.FromMilliseconds(7));

        return summaryMock;
    }

    [Fact]
    public void Maps_all_counter_fields_without_transposing_them()
    {
        var summary = CreateSummaryMock();

        var result = _mapper.Map(summary.Object);

        result.Counters.Should().Be(
            new SummaryCountersResponse(true, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, true));
    }

    [Fact]
    public void Maps_server_info()
    {
        var summary = CreateSummaryMock();

        var result = _mapper.Map(summary.Object);

        result.ServerInfo.Should().Be(new SummaryServerInfoResponse("localhost:7687", "Neo4j/5.20.0", "5.4"));
    }

    [Fact]
    public void Maps_query_text_and_cypher_converts_the_parameters()
    {
        var summary = CreateSummaryMock();
        summary.Setup(s => s.Query).Returns(new Query("RETURN $x", new Dictionary<string, object> { ["x"] = 1L }));
        _cypherMapperMock.Setup(m => m.Map(1L)).Returns(new CypherInt(1));

        var result = _mapper.Map(summary.Object);

        result.Query.Text.Should().Be("RETURN $x");
        result.Query.Parameters.Should().Equal(new Dictionary<string, ICypherValue> { ["x"] = new CypherInt(1) });
    }

    [Theory]
    [InlineData(QueryType.ReadOnly, "r")]
    [InlineData(QueryType.ReadWrite, "rw")]
    [InlineData(QueryType.WriteOnly, "w")]
    [InlineData(QueryType.SchemaWrite, "s")]
    [InlineData(QueryType.Unknown, null)]
    public void Maps_query_type(QueryType queryType, string? expected)
    {
        var summary = CreateSummaryMock();
        summary.Setup(s => s.QueryType).Returns(queryType);

        _mapper.Map(summary.Object).QueryType.Should().Be(expected);
    }

    [Fact]
    public void Maps_a_negative_available_after_to_null()
    {
        var summary = CreateSummaryMock();
        summary.Setup(s => s.ResultAvailableAfter).Returns(TimeSpan.FromMilliseconds(-1));

        _mapper.Map(summary.Object).ResultAvailableAfter.Should().BeNull();
    }

    [Fact]
    public void Maps_database_name_and_null_when_there_is_no_database_name()
    {
        var summary = CreateSummaryMock();
        summary.Setup(s => s.Database).Returns(new DatabaseInfo(null));

        _mapper.Map(summary.Object).Database.Should().BeNull();
    }

    [Fact]
    public void Maps_an_empty_list_when_notifications_is_null()
    {
        var summary = CreateSummaryMock();
        summary.Setup(s => s.Notifications).Returns((IList<INotification>)null!);

        _mapper.Map(summary.Object).Notifications.Should().BeEmpty();
    }

    [Fact]
    public void Maps_an_empty_list_when_gql_status_objects_is_null()
    {
        var summary = CreateSummaryMock();
        summary.Setup(s => s.GqlStatusObjects).Returns((IList<IGqlStatusObject>)null!);

        _mapper.Map(summary.Object).GqlStatusObjects.Should().BeEmpty();
    }

    [Fact]
    public void Maps_a_notification_without_a_position()
    {
        var notification = new Notification(
            "Neo.ClientNotification.Some.Hint",
            "A hint",
            "a hint",
            null,
            "WARNING",
            "HINT");

        var summary = CreateSummaryMock();
        summary.Setup(s => s.Notifications).Returns(new List<INotification> { notification });

        var result = _mapper.Map(summary.Object);

        result.Notifications.Should().Equal(
            new SummaryNotificationResponse(
                "HINT",
                "HINT",
                "WARNING",
                "WARNING",
                "a hint",
                "Neo.ClientNotification.Some.Hint",
                "A hint",
                null));
    }

    [Fact]
    public void A_notification_without_a_position_omits_it_from_the_wire_instead_of_sending_null()
    {
        var notification = new SummaryNotificationResponse(
            "HINT",
            "HINT",
            "WARNING",
            "WARNING",
            "a hint",
            "Neo.ClientNotification.Some.Hint",
            "A hint",
            null);

        var json = JsonSerializer.Serialize(notification, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        json.Should().NotContain("position");
    }

    [Fact]
    public void Maps_a_notification_with_a_position()
    {
        var position = new InputPosition(offset: 10, line: 1, column: 3);
        var notification = new Notification(
            "Neo.ClientNotification.Some.Deprecation",
            "Deprecated",
            "deprecated",
            position,
            "WARNING",
            "DEPRECATION");

        var summary = CreateSummaryMock();
        summary.Setup(s => s.Notifications).Returns(new List<INotification> { notification });

        var result = _mapper.Map(summary.Object);

        result.Notifications.Should().ContainSingle().Which.Position.Should().Be(
            new SummaryPositionResponse(3, 10, 1));
    }

    [Fact]
    public void Does_not_map_a_plan_or_profile_when_absent()
    {
        var summary = CreateSummaryMock();

        var result = _mapper.Map(summary.Object);

        result.Plan.Should().BeNull();
        result.Profile.Should().BeNull();
    }

    [Fact]
    public void Maps_a_plan_recursively()
    {
        var childPlan = new Plan("NodeByLabelScan", new Dictionary<string, object>(), ["n"], []);
        var rootPlan = new Plan(
            "ProduceResults",
            new Dictionary<string, object> { ["planner"] = "COST" },
            ["n"],
            [childPlan]);

        var summary = CreateSummaryMock();
        summary.Setup(s => s.HasPlan).Returns(true);
        summary.Setup(s => s.Plan).Returns(rootPlan);

        var result = _mapper.Map(summary.Object);

        result.Plan.Should().BeEquivalentTo(
            new SummaryPlanResponse(
                new Dictionary<string, object> { ["planner"] = "COST" },
                "ProduceResults",
                [new SummaryPlanResponse(new Dictionary<string, object>(), "NodeByLabelScan", [], ["n"])],
                ["n"]),
            opts => opts.ComparingByMembers<SummaryPlanResponse>());
    }

    [Fact]
    public void Maps_a_profile_recursively_with_its_stats()
    {
        var profile = new QueryProfile(
            "ProduceResults",
            new Dictionary<string, object>(),
            [],
            [],
            dbHits: 42L,
            rows: 1L,
            pageCacheHits: 2L,
            pageCacheMisses: 3L,
            pageCacheHitRatio: 0.5,
            time: 9L);

        var summary = CreateSummaryMock();
        summary.Setup(s => s.HasProfile).Returns(true);
        summary.Setup(s => s.QueryProfile).Returns(profile);

        var result = _mapper.Map(summary.Object);

        result.Profile.Should().BeEquivalentTo(
            new SummaryProfileResponse(
                new Dictionary<string, object>(),
                "ProduceResults",
                [],
                [],
                9L,
                0.5,
                3L,
                2L,
                1L,
                42L),
            opts => opts.ComparingByMembers<SummaryProfileResponse>());
    }

    [Fact]
    public void Maps_a_gql_status_object()
    {
        // Only the internal concrete type exposes IsNotification - IGqlStatusObject doesn't.
        var statusObject = new GqlStatusObject(
            "00000",
            "note: successful completion",
            null!,
            null!,
            null!,
            new Dictionary<string, object> { ["OPERATION"] = "" },
            null!,
            false);

        _cypherMapperMock.Setup(m => m.Map("")).Returns(new CypherString(""));

        var summary = CreateSummaryMock();
        summary.Setup(s => s.GqlStatusObjects).Returns(new List<IGqlStatusObject> { statusObject });

        var result = _mapper.Map(summary.Object);

        result.GqlStatusObjects.Should().BeEquivalentTo(
            [new SummaryGqlStatusObjectResponse(
                "00000",
                "note: successful completion",
                new Dictionary<string, ICypherValue> { ["OPERATION"] = new CypherString("") },
                "UNKNOWN",
                null,
                null,
                "UNKNOWN",
                null,
                false)],
            opts => opts.ComparingByMembers<SummaryGqlStatusObjectResponse>());
    }
}
