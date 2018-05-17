// Copyright (c) 2002-2018 "Neo4j,"
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
using Neo4j.Driver.V1;
using FluentAssertions;
using Neo4j.Driver.Internal.Types;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests.Types
{
    public class PointsIT: DirectDriverTestBase
    {
        private const int WGS84SrId = 4326;
        private const int WGS843DSrId = 4979;
        private const int CartesianSrId = 7203;
        private const int Cartesian3DSrId = 9157;

        private readonly Random _random = new Random();

        public PointsIT(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
            : base(output, fixture)
        {

        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.4.0")]
        public void ShouldReceive()
        {
            using (var session = Server.Driver.Session(AccessMode.Read))
            {
                var record = session.Run("RETURN point({x: 39.111748, y:-76.775635}), point({x: 39.111748, y:-76.775635, z:35.120})").Single();
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
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.4.0")]
        public void ShouldSend()
        {
            using (var session = Server.Driver.Session(AccessMode.Read))
            {
                var point1 = new Point(WGS84SrId, 51.5044585, -0.105658);
                var point2 = new Point(WGS843DSrId, 51.5044585, -0.105658, 35.120);
                var created = session.Run("CREATE (n:Node { location1: $point1, location2: $point2 }) RETURN 1", new {point1, point2}).Single();

                created[0].Should().Be(1L);

                var matched = session.Run("MATCH (n:Node) RETURN n.location1, n.location2").Single();

                matched[0].ShouldBeEquivalentTo(point1);
                matched[1].ShouldBeEquivalentTo(point2);
            }
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.4.0")]
        public void ShouldSendAndReceive()
        {
            TestSendAndReceive(new Point(WGS84SrId, 51.24923585, 0.92723724));
            TestSendAndReceive(new Point(WGS843DSrId, 22.86211019, 171.61820439, 0.1230987));
            TestSendAndReceive(new Point(CartesianSrId, 39.111748, -76.775635));
            TestSendAndReceive(new Point(Cartesian3DSrId, 39.111748, -76.775635, 19.2937302840));
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.4.0")]
        public void ShouldSendAndReceiveRandom()
        {
            var randomPoints = Enumerable.Range(0, 1000).Select(GenerateRandomPoint).ToList();

            randomPoints.ForEach(TestSendAndReceive);
        }

        [RequireServerVersionGreaterThanOrEqualToFact("3.4.0")]
        public void ShouldSendAndReceiveListRandom()
        {
            var randomPointLists = Enumerable.Range(0, 1000).Select(i => GenerateRandomPointList(i, 100)).ToList();

            randomPointLists.ForEach(TestSendAndReceiveList);
        }

        private void TestSendAndReceive(Point point)
        {
            using (var session = Server.Driver.Session(AccessMode.Read))
            {
                var result = 
                    session.Run("CREATE (n { point: $point}) RETURN n.point", new {point}).Single();

                result[0].ShouldBeEquivalentTo(point);
            }
        }

        private void TestSendAndReceiveList(IEnumerable<Point> points)
        {
            using (var session = Server.Driver.Session(AccessMode.Read))
            {
                var result =
                    session.Run("CREATE (n { points: $points}) RETURN n.points", new { points }).Single();

                result[0].ShouldBeEquivalentTo(points);
            }
        }

        private IEnumerable<Point> GenerateRandomPointList(int sequence, int count)
        {
            return Enumerable.Range(0, count).Select(i => GenerateRandomPoint(sequence)).ToList();
        }

        private Point GenerateRandomPoint(int sequence)
        {
            switch (sequence % 4)
            {
                case 0:
                    return new Point(WGS84SrId, GenerateRandomDouble(), GenerateRandomDouble());
                case 1:
                    return new Point(WGS843DSrId, GenerateRandomDouble(), GenerateRandomDouble(),
                        GenerateRandomDouble());
                case 2:
                    return new Point(CartesianSrId, GenerateRandomDouble(), GenerateRandomDouble());
                case 3:
                    return new Point(Cartesian3DSrId, GenerateRandomDouble(), GenerateRandomDouble(),
                        GenerateRandomDouble());
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private double GenerateRandomDouble()
        {
            return _random.Next(-179, 179) + _random.NextDouble();
        }

    }
}