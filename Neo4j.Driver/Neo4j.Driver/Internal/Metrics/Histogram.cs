// Copyright (c) 2002-2018 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
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
using System.IO;
using HdrHistogram;

namespace Neo4j.Driver.Internal.Metrics
{
    internal class Histogram : IHistogram
    {
        private readonly LongConcurrentHistogram _histogram;
        public Histogram(LongConcurrentHistogram histgram)
        {
            _histogram = histgram;
        }

        public long Max => _histogram.GetMaxValue();
        public double Mean => _histogram.GetMean();
        public double StdDeviation => _histogram.GetStdDeviation();
        public double TotalCount => _histogram.TotalCount;
        public long GetValueAtPercentile(double percentile) => _histogram.GetValueAtPercentile(percentile);
        public void RecordValue(long value) => _histogram.RecordValue(value);

        public void Reset() => _histogram.Reset();

        public override string ToString()
        {
            var writer = new StringWriter();
            _histogram.OutputPercentileDistribution(writer);
            return writer.ToString();
        }
    }
}
