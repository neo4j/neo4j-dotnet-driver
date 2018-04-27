// Copyright (c) 2002-2018 Neo4j Sweden AB [http://neo4j.com]
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
    internal class LeastConnectedLoadBalancingStrategy : ILoadBalancingStrategy
    {
        private readonly RoundRobinArrayIndex _readersIndex = new RoundRobinArrayIndex();
        private readonly RoundRobinArrayIndex _writersIndex = new RoundRobinArrayIndex();

        private readonly IClusterConnectionPool _connectionPool;
        private readonly ILogger _logger;

        public LeastConnectedLoadBalancingStrategy(IClusterConnectionPool connectionPool, ILogger logger)
        {
            _connectionPool = connectionPool;
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
                _logger.Trace($"Unable to select {addressType}, no known addresses given");
                return null;
            }

            // choose start index for iteration in round-rodin fashion
            var startIndex = roundRobinIndex.Next(count);
            var index = startIndex;

            Uri leastConnectedAddress = null;
            var leastActiveConnections = Int32.MaxValue;

            // iterate over the array to find least connected address
            do
            {
                Uri address = addresses[index];
                int inUseConnections = _connectionPool.NumberOfInUseConnections(address);

                if (inUseConnections < leastActiveConnections)
                {
                    leastConnectedAddress = address;
                    leastActiveConnections = inUseConnections;
                }

                // loop over to the start of the array when end is reached
                if (index == count - 1)
                {
                    index = 0;
                }
                else
                {
                    index++;
                }
            } while (index != startIndex);

            _logger.Trace(
                $"Selected {addressType} with address: '{leastConnectedAddress}' and active connections: {leastActiveConnections}");

            return leastConnectedAddress;
        }
    }
}
