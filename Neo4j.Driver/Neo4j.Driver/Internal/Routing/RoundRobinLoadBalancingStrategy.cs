﻿// Copyright (c) "Neo4j"
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
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Routing
{
    internal class RoundRobinLoadBalancingStrategy : ILoadBalancingStrategy
    {
        private readonly RoundRobinArrayIndex _readersIndex = new RoundRobinArrayIndex();
        private readonly RoundRobinArrayIndex _writersIndex = new RoundRobinArrayIndex();

        private readonly IDriverLogger _logger;

        public RoundRobinLoadBalancingStrategy(IDriverLogger logger)
        {
            _logger = logger;
        }

        public Uri SelectReader(IList<Uri> knownReaders)
        {
            return Select(knownReaders, _readersIndex, "reader");
        }

        public Uri SelectWriter(IList<Uri> knownWriters)
        {
            return Select(knownWriters, _writersIndex, "writer");
        }

        private Uri Select(IList<Uri> addresses, RoundRobinArrayIndex roundRobinIndex, string addressType)
        {
            var count = addresses.Count;
            if (count == 0)
            {
                LogDebug($"Unable to select {addressType}, no known addresses given.");
                return null;
            }

            var index = roundRobinIndex.Next(count);
            var address = addresses[index];
            LogDebug($"Selected {addressType} with address: '{address}' in round-robin fashion.");
            return address;
        }

        private void LogDebug(string message)
        {
            if (_logger.IsDebugEnabled())
            {
                _logger.Debug(message);
            }
        }
    }
}
