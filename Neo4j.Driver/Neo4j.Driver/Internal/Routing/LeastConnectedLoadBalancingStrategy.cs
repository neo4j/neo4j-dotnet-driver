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

using System;
using System.Collections.Generic;

namespace Neo4j.Driver.Internal.Routing;

internal class LeastConnectedLoadBalancingStrategy : ILoadBalancingStrategy
{
    private readonly IClusterConnectionPool _connectionPool;
    private readonly ILogger _logger;
    private readonly RoundRobinArrayIndex _readersIndex = new();
    private readonly RoundRobinArrayIndex _writersIndex = new();

    public LeastConnectedLoadBalancingStrategy(IClusterConnectionPool connectionPool, ILogger logger)
    {
        _connectionPool = connectionPool;
        _logger = logger;
    }

    public Uri SelectReader(IList<Uri> knownReaders, string forDatabase)
    {
        return Select(knownReaders, _readersIndex, forDatabase, "reader");
    }

    public Uri SelectWriter(IList<Uri> knownWriters, string forDatabase)
    {
        return Select(knownWriters, _writersIndex, forDatabase, "writer");
    }

    private Uri Select(
        IList<Uri> addresses,
        RoundRobinArrayIndex roundRobinIndex,
        string forDatabase,
        string addressType)
    {
        var count = addresses.Count;
        if (count == 0)
        {
            if (_logger.IsDebugEnabled())
            {
                _logger.Debug($"Unable to select {addressType} for database '{forDatabase}', no known addresses given");
            }
            return null;
        }

        // choose start index for iteration in round-robin fashion
        var startIndex = roundRobinIndex.Next(count);
        var index = startIndex;

        Uri leastConnectedAddress = null;
        var leastActiveConnections = int.MaxValue;

        // iterate over the array to find least connected address
        do
        {
            var address = addresses[index];
            var inUseConnections = _connectionPool.NumberOfInUseConnections(address);

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

        if (_logger.IsDebugEnabled())
        {
            _logger.Debug($"Selected {addressType} for database '{forDatabase}' with least connected address: '{leastConnectedAddress}' and active connections: {leastActiveConnections}");   
        }
        return leastConnectedAddress;
    }

}
