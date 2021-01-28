// Copyright (c) "Neo4j"
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
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Metrics;
using Xunit;
using static System.Double;

namespace Neo4j.Driver.Tests.Metrics
{
    public class HistogramTests
    {
        [Fact]
        public void EmptyHistogramShouldReturnValuesAsExpected()
        {
            var histogram = new Histogram();

            histogram.TotalCount.Should().Be(0);
            histogram.Max.Should().Be(0);
            histogram.Mean.Should().Be(NaN);
            histogram.StdDeviation.Should().Be(NaN);

            var exception = Record.Exception(()=>histogram.GetValueAtPercentile(50));
            exception.Should().BeOfType<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void HistogramShouldReturnValuesAsExpected()
        {
            var histogram = new Histogram();
            histogram.RecordValue(0);

            histogram.TotalCount.Should().Be(1);
            histogram.Max.Should().Be(0);
            histogram.Mean.Should().Be(0);
            histogram.StdDeviation.Should().Be(0);

            histogram.GetValueAtPercentile(50).Should().Be(0);
        }

        [Fact]
        public void HistogramWithMultipleSmallValuesShouldReturnValueAsExpected()
        {
            var histogram = new Histogram();
            histogram.RecordValue(0);
            histogram.RecordValue(1);
            histogram.RecordValue(2);
            histogram.RecordValue(3);
            histogram.RecordValue(4);

            histogram.TotalCount.Should().Be(5);
            histogram.Max.Should().Be(4);
            histogram.Mean.Should().Be(2);
            histogram.StdDeviation.Should().BeGreaterThan(0);

            histogram.GetValueAtPercentile(50).Should().Be(2);
        }

        [Fact]
        public void HistogramWithMultipleBigValuesShouldReturnValueAsExpected()
        {
            var histogram = new Histogram();
            histogram.RecordValue(0);
            histogram.RecordValue(10000);
            histogram.RecordValue(20000);
            histogram.RecordValue(30000);
            histogram.RecordValue(40000);

            histogram.TotalCount.Should().Be(5);
            (histogram.Max-40000).Should().BeLessThan(40); // 3 significant decimal, a.k.a. 40000 * 0.001
            (histogram.Mean-20000).Should().BeLessThan(20);
            histogram.StdDeviation.Should().BeGreaterThan(0);

            (histogram.GetValueAtPercentile(50)-20000).Should().BeLessThan(20);
        }

        [Fact]
        public void ShouldResetHistogram()
        {
            var histogram = new Histogram();
            histogram.RecordValue(0);
            histogram.RecordValue(1);
            histogram.RecordValue(2);
            histogram.RecordValue(3);
            histogram.RecordValue(4);

            histogram.Reset();
            histogram.RecordValue(1);
            histogram.TotalCount.Should().Be(1);
            histogram.Max.Should().Be(1);
            histogram.Mean.Should().Be(1);
            histogram.StdDeviation.Should().Be(0);

            histogram.GetValueAtPercentile(50).Should().Be(1);
        }
    }

    public class HistogramSnapshotTests
    {
        [Fact]
        public void ReadonlyOperationsShouldDelegateToReadonlyHistogram()
        {
            var copy = new Mock<IHistogram>();
            var origin = new Mock<IHistogram>();
            var histogram = new HistogramSnapshot(copy.Object, origin.Object);

            var max = histogram.Max;
            copy.Verify(c => c.Max, Times.Once);
            var mean = histogram.Mean;
            copy.Verify(c => c.Mean, Times.Once);
            var std = histogram.StdDeviation;
            copy.Verify(c => c.StdDeviation, Times.Once);

            var count = histogram.TotalCount;
            copy.Verify(c => c.TotalCount, Times.Once);

            histogram.GetValueAtPercentile(50);
            copy.Verify(c => c.GetValueAtPercentile(50), Times.Once);
        }

        [Fact]
        public void WriteOperationShouldDelegateToOriginHistogram()
        {
            var copy = new Mock<IHistogram>();
            var origin = new Mock<IHistogram>();
            var histogram = new HistogramSnapshot(copy.Object, origin.Object);

            histogram.Reset();
            origin.Verify(o => o.Reset(), Times.Once);
        }
    }
}
