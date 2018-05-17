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
using System.IO;
using HdrHistogram;

namespace Neo4j.Driver.Internal.Metrics
{
    internal class Histogram : IHistogram
    {
        // The fileds that will be written out in ToString
        public long Max => _hdrHistogram.GetMaxValue();
        public double Mean => _hdrHistogram.GetMean();
        public double StdDeviation => _hdrHistogram.GetStdDeviation();
        public long TotalCount => _hdrHistogram.TotalCount;

        private readonly HistogramBase _hdrHistogram;

        public Histogram(long highestTrackableValueInTicks = -1)
        {
            if (highestTrackableValueInTicks < 0)
            {
                highestTrackableValueInTicks = DefaultHighestTrackableInTicks;
            }
            _hdrHistogram = CreateHdrHistogram(highestTrackableValueInTicks);
        }

        private Histogram(HistogramBase hdrHdrHistogram)
        {
            _hdrHistogram = hdrHdrHistogram;
        }

        public long GetValueAtPercentile(double percentile)
        {
            return _hdrHistogram.GetValueAtPercentile(percentile);
        }

        public void RecordValue(long value)
        {
            var newValue = TruncateValue(value, _hdrHistogram);
            _hdrHistogram.RecordValue(newValue);
        }

        public void Reset()
        {
            _hdrHistogram.Reset();
        }

        public IHistogram Snapshot()
        {
            return new HistogramSnapshot(new Histogram(_hdrHistogram.Copy()), this);
        }

        public override string ToString()
        {
            var writer = new StringWriter();
            _hdrHistogram.OutputPercentileDistribution(writer);
            return writer.ToString();
        }

        // TODO: consider to make this configurable
        private static readonly long DefaultHighestTrackableInTicks = TimeSpan.FromMinutes(10).Ticks;

        /// <summary>
        /// This method creates a HdrHistogram where the minimal accuracy is 1 tick, a.k.a. 100 nanoseconds, or 0.1 microseconds.
        /// The significant decimal digits is 3 decimal across the whole range.
        /// </summary>
        /// <param name="highestTrackableValueInTicks">The highest accuracy of this histogram.</param>
        /// <returns>A concurrent HdrHistogram of long values.</returns>
        private static LongConcurrentHistogram CreateHdrHistogram(long highestTrackableValueInTicks)
        {
            return new LongConcurrentHistogram(1, highestTrackableValueInTicks, 3);
        }

        private static long TruncateValue(long value, HistogramBase histogram)
        {
            if (value > histogram.HighestTrackableValue)
            {
                return histogram.HighestTrackableValue;
            }
            else
            {
                return value;
            }
        }
    }

    /// <summary>
    /// The HistogramSnapshot not only is a readonly histogram but also allows reset operation on the original histogram
    /// where the readonly histogram is created.
    /// </summary>
    internal class HistogramSnapshot : IHistogram
    {
        private readonly IHistogram _origin;
        private readonly IHistogram _copy;

        public HistogramSnapshot(IHistogram copy, IHistogram origin)
        {
            _copy = copy;
            _origin = origin;
        }

        public long Max => _copy.Max;
        public double Mean => _copy.Mean;
        public double StdDeviation => _copy.StdDeviation;
        public long TotalCount => _copy.TotalCount;
        public long GetValueAtPercentile(double percentile) => _copy.GetValueAtPercentile(percentile);
        public void Reset() => _origin.Reset();
        public override string ToString() => _copy.ToString();
    }
}
