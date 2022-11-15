// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License"):
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

using System.Threading;

namespace Neo4j.Driver.Internal.Routing;

internal class RoundRobinArrayIndex
{
    private const int InitialValue = -1;

    private int _offset;

    public RoundRobinArrayIndex()
    {
        _offset = InitialValue;
    }

    // only for testing
    internal RoundRobinArrayIndex(int initialOffset)
    {
        _offset = initialOffset;
    }

    public int Next(int arrayLength)
    {
        if (arrayLength == 0)
        {
            return -1;
        }

        int nextOffset;
        while ((nextOffset = Interlocked.Increment(ref _offset)) < 0)
        {
            // overflow, try resetting back to zero
            Interlocked.CompareExchange(ref _offset, InitialValue, nextOffset);
        }

        return nextOffset % arrayLength;
    }
}
