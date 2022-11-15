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

using System.Collections.Generic;
using FluentAssertions;
using Neo4j.Driver.Internal.Types;
using Xunit;

namespace Neo4j.Driver.Tests.Types;

public class PointTests
{
    [Fact]
    public void ShouldCreate2DPoints()
    {
        var point = new Point(1, 2.0, 3.0);

        point.Dimension.Should().Be(2);
        point.SrId.Should().Be(1);
        point.X.Should().Be(2.0);
        point.Y.Should().Be(3.0);
    }

    [Fact]
    public void ShouldCreate3DPoints()
    {
        var point = new Point(1, 2.0, 3.0, 4.0);

        point.Dimension.Should().Be(3);
        point.SrId.Should().Be(1);
        point.X.Should().Be(2.0);
        point.Y.Should().Be(3.0);
        point.Z.Should().Be(4.0);
    }

    [Fact]
    public void ShouldCreate3DPointsWithNan()
    {
        var point = new Point(1, 2.0, 3.0, double.NaN);

        point.Dimension.Should().Be(3);
        point.SrId.Should().Be(1);
        point.X.Should().Be(2.0);
        point.Y.Should().Be(3.0);
        point.Z.Should().Be(double.NaN);
    }

    [Fact]
    public void ShouldGenerateCorrectStringWhen2D()
    {
        var point = new Point(1, 135.37340722, 11.92245761);
        var pointStr = point.ToString();

        pointStr.Should().Be("Point{srId=1, x=135.37340722, y=11.92245761}");
    }

    [Fact]
    public void ShouldGenerateCorrectStringWhen3D()
    {
        var point = new Point(1, 135.37340722, 11.92245761, 35.1201208);
        var pointStr = point.ToString();

        pointStr.Should().Be("Point{srId=1, x=135.37340722, y=11.92245761, z=35.1201208}");
    }

    [Fact]
    public void ShouldGenerateDifferentHashCodes2D()
    {
        var point1 = new Point(1, 135.37340722, 11.92245761);
        var point2 = new Point(1, 135.37340722, 11.92245762);

        point1.GetHashCode().Should().NotBe(point2.GetHashCode());
    }

    [Fact]
    public void ShouldGenerateIdenticalHashCodes2D()
    {
        var point1 = new Point(1, 135.37340722, 11.92245761);
        var point2 = new Point(1, 135.37340722, 11.92245761);

        point1.GetHashCode().Should().Be(point2.GetHashCode());
    }

    [Fact]
    public void ShouldGenerateDifferentHashCodes3D()
    {
        var point1 = new Point(1, 135.37340722, 11.92245761, 35.1201208);
        var point2 = new Point(1, 135.37340722, 11.92245761, 35.1201209);

        point1.GetHashCode().Should().NotBe(point2.GetHashCode());
    }

    [Fact]
    public void ShouldGenerateIdenticalHashCodes3D()
    {
        var point1 = new Point(1, 135.37340722, 11.92245761, 35.1201208);
        var point2 = new Point(1, 135.37340722, 11.92245761, 35.1201208);

        point1.GetHashCode().Should().Be(point2.GetHashCode());
    }

    [Fact]
    public void ShouldGenerateIdenticalHashCodes3DWhenZisNaN()
    {
        var point1 = new Point(1, 135.37340722, 11.92245761, double.NaN);
        var point2 = new Point(1, 135.37340722, 11.92245761, double.NaN);

        point1.GetHashCode().Should().Be(point2.GetHashCode());
    }

    [Fact]
    public void ShouldGenerateDifferentHashCodes2DAnd3D()
    {
        var point1 = new Point(1, 135.37340722, 11.92245761);
        var point2 = new Point(1, 135.37340722, 11.92245761, double.NaN);

        point1.GetHashCode().Should().NotBe(point2.GetHashCode());
    }

    [Fact]
    public void ShouldNotBeEqual2D()
    {
        var point1 = new Point(1, 135.37340722, 11.92245761);
        var point2 = new Point(1, 135.37340722, 11.92245762);

        point1.Equals(point2).Should().BeFalse();
    }

    [Fact]
    public void ShouldBeEqual2D()
    {
        var point1 = new Point(1, 135.37340722, 11.92245761);
        var point2 = new Point(1, 135.37340722, 11.92245761);

        point1.Equals(point2).Should().BeTrue();
    }

    [Fact]
    public void ShouldNotBeEqual3D()
    {
        var point1 = new Point(1, 135.37340722, 11.92245761, 35.1201208);
        var point2 = new Point(1, 135.37340722, 11.92245761, 35.1201209);

        point1.Equals(point2).Should().BeFalse();
    }

    [Fact]
    public void ShouldBeEqual3D()
    {
        var point1 = new Point(1, 135.37340722, 11.92245761, 35.1201208);
        var point2 = new Point(1, 135.37340722, 11.92245761, 35.1201208);

        point1.Equals(point2).Should().BeTrue();
    }

    [Fact]
    public void ShouldBeEqual3DWhenZisNaN()
    {
        var point1 = new Point(1, 135.37340722, 11.92245761, double.NaN);
        var point2 = new Point(1, 135.37340722, 11.92245761, double.NaN);

        point1.Equals(point2).Should().BeTrue();
    }

    [Fact]
    public void ShouldNotBeEqual2DAnd3D()
    {
        var point1 = new Point(1, 135.37340722, 11.92245761);
        var point2 = new Point(1, 135.37340722, 11.92245761, double.NaN);

        point1.Equals(point2).Should().BeFalse();
    }

    [Fact]
    public void ShouldNotBeEqualToNull()
    {
        var point1 = new Point(1, 135.37340722, 11.92245761);

        point1.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void ShouldNotBeEqualToOtherType()
    {
        var point1 = new Point(1, 135.37340722, 11.92245761);

        point1.Equals(new Node(1, new List<string>(), new Dictionary<string, object>())).Should().BeFalse();
        point1.Equals(1).Should().BeFalse();
    }
}
