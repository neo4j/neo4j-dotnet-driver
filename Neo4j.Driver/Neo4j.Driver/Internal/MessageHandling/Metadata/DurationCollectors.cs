// Copyright (c) 2002-2020 "Neo4j,"
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

namespace Neo4j.Driver.Internal.MessageHandling.Metadata
{
    internal abstract class DurationCollector : IMetadataCollector<long>
    {
        private readonly string _key;

        protected DurationCollector(string key)
        {
            _key = key;
        }

        object IMetadataCollector.Collected => Collected;

        public long Collected { get; private set; } = -1;

        public void Collect(IDictionary<string, object> metadata)
        {
            if (metadata != null && metadata.TryGetValue(_key, out var durationValue))
            {
                if (durationValue is long duration)
                {
                    Collected = duration;
                }
                else
                {
                    throw new ProtocolException(
                        $"Expected '{_key}' metadata to be of type 'Int64', but got '{durationValue?.GetType().Name}'.");
                }
            }
        }
    }

    internal class TimeToFirstCollector : DurationCollector
    {
        internal const string TimeToFirstKey = "t_first";

        public TimeToFirstCollector()
            : base(TimeToFirstKey)
        {
        }
    }

    internal class TimeToLastCollector : DurationCollector
    {
        internal const string TimeToLastKey = "t_last";

        public TimeToLastCollector()
            : base(TimeToLastKey)
        {
        }
    }
}