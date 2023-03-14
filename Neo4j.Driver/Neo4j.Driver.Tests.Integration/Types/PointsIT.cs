// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.IntegrationTests.Direct;
using Neo4j.Driver.IntegrationTests.Internals;
using Xunit.Abstractions;
using static Neo4j.Driver.IntegrationTests.Internals.VersionComparison;

namespace Neo4j.Driver.IntegrationTests.Types;

public sealed class PointsIT : DirectDriverTestBase
{
    private const int Wgs84SrId = 4326;
    private const int Wgs843DSrId = 4979;
    private const int CartesianSrId = 7203;
    private const int Cartesian3DSrId = 9157;

    private readonly Random _random = new();

    public PointsIT(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
        : base(output, fixture)
    {
    }

    [RequireServerFact("3.4.0", GreaterThanOrEqualTo)]
    public async Task ShouldReceive()
    {
        var session = Server.Driver.AsyncSession(o => o.WithDefaultAccessMode(AccessMode.Read));
        try
        {
            var cursor = await session
                .RunAsync("RETURN point({x: 39.111748, y:-76.775635}), point({x: 39.111748, y:-76.775635, z:35.120})");

            var record = await cursor.SingleAsync();
            var point1 = record[0];
            var point2 = record[1];

            point1.Should().NotBeNull();
            point1.Should().BeOfType<Point>().Which.SrId.Should().Be(CartesianSrId);
            point1.Should().BeOfType<Point>().Which.X.Should().Be(39.111748);
            point1.Should().BeOfType<Point>().Which.Y.Should().Be(-76.775635);
            point1.Should().BeOfType<Point>().Which.Z.Should().Be(double.NaN);

            point2.Should().NotBeNull();
            point2.Should().BeAssignableTo<Point>().Which.SrId.Should().Be(Cartesian3DSrId);
            point2.Should().BeAssignableTo<Point>().Which.X.Should().Be(39.111748);
            point2.Should().BeAssignableTo<Point>().Which.Y.Should().Be(-76.775635);
            point2.Should().BeAssignableTo<Point>().Which.Z.Should().Be(35.120);
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    [RequireServerFact("3.4.0", GreaterThanOrEqualTo)]
    public async Task ShouldSend()
    {
        var session = Server.Driver.AsyncSession(o => o.WithDefaultAccessMode(AccessMode.Write));
        try
        {
            var point1 = new Point(Wgs84SrId, 51.5044585, -0.105658);
            var point2 = new Point(Wgs843DSrId, 51.5044585, -0.105658, 35.120);
            var createdCursor = await session.RunAsync(
                "CREATE (n:Node { location1: $point1, location2: $point2 }) RETURN 1",
                new { point1, point2 });

            var created = await createdCursor.SingleAsync();

            created[0].Should().Be(1L);

            var matchedCursor = await session.RunAsync("MATCH (n:Node) RETURN n.location1, n.location2");
            var matched = await matchedCursor.SingleAsync();

            matched[0].Should().BeEquivalentTo(point1);
            matched[1].Should().BeEquivalentTo(point2);
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    [RequireServerFact("3.4.0", GreaterThanOrEqualTo)]
    public async Task ShouldSendAndReceive()
    {
        await TestSendAndReceive(new Point(Wgs84SrId, 51.24923585, 0.92723724));
        await TestSendAndReceive(new Point(Wgs843DSrId, 22.86211019, 71.61820439, 0.1230987));
        await TestSendAndReceive(new Point(CartesianSrId, 39.111748, -76.775635));
        await TestSendAndReceive(new Point(Cartesian3DSrId, 39.111748, -76.775635, 19.2937302840));
    }

    [RequireServerFact("3.4.0", GreaterThanOrEqualTo)]
    public async Task ShouldSendAndReceiveRandom()
    {
        await Task.WhenAll(Enumerable.Range(0, 1000).Select(GenerateRandomPoint).Select(TestSendAndReceive));
    }

    [RequireServerFact("3.4.0", GreaterThanOrEqualTo)]
    public async Task ShouldSendAndReceiveListRandom()
    {
        await Task.WhenAll(
            Enumerable.Range(0, 1000).Select(i => GenerateRandomPointList(i, 100)).Select(TestSendAndReceiveList));
    }

    private async Task TestSendAndReceive(Point point)
    {
        var session = Server.Driver.AsyncSession(o => o.WithDefaultAccessMode(AccessMode.Write));
        try
        {
            var cursor = await
                session.RunAsync("CREATE (n { point: $point}) RETURN n.point", new { point });

            var result = await cursor.SingleAsync();

            result[0].Should().BeEquivalentTo(point);
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    private async Task TestSendAndReceiveList(IList<Point> points)
    {
        var session = Server.Driver.AsyncSession(o => o.WithDefaultAccessMode(AccessMode.Write));
        try
        {
            var cursor =
                await session.RunAsync("CREATE (n { points: $points}) RETURN n.points", new { points });

            var result = await cursor.SingleAsync();

            result[0].Should().BeEquivalentTo(points);
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    private IList<Point> GenerateRandomPointList(int sequence, int count)
    {
        return Enumerable.Range(0, count).Select(_ => GenerateRandomPoint(sequence)).ToList();
    }

    private Point GenerateRandomPoint(int sequence)
    {
        return (sequence % 4) switch
        {
            0 => new Point(Wgs84SrId, GenerateRandomX(), GenerateRandomY()),
            1 => new Point(Wgs843DSrId, GenerateRandomX(), GenerateRandomY(), GenerateRandomZ()),
            2 => new Point(CartesianSrId, GenerateRandomX(), GenerateRandomY()),
            3 => new Point(Cartesian3DSrId, GenerateRandomX(), GenerateRandomY(), GenerateRandomZ()),
            var _ => throw new ArgumentOutOfRangeException()
        };
    }

    private double GenerateRandomX()
    {
        return GenerateRandomDouble(-179, 179);
    }

    private double GenerateRandomY()
    {
        return GenerateRandomDouble(-89, 89);
    }

    private double GenerateRandomZ()
    {
        return GenerateRandomDouble(0, 100);
    }

    private double GenerateRandomDouble(int min, int max)
    {
        return _random.Next(min, max) + _random.NextDouble();
    }
}
