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

namespace Neo4j.Driver.Internal;

internal static class BufferSettings
{
    public static void Validate(Config config)
    {
        var defaultReadBufferSize = config.DefaultReadBufferSize;
        var maxReadBufferSize = config.MaxReadBufferSize;
        var defaultWriteBufferSize = config.DefaultWriteBufferSize;
        var maxWriteBufferSize = config.MaxWriteBufferSize;
        Throw.ArgumentOutOfRangeException.IfValueLessThan(defaultReadBufferSize, 0, nameof(defaultReadBufferSize));
        Throw.ArgumentOutOfRangeException.IfValueLessThan(maxReadBufferSize, 0, nameof(maxReadBufferSize));
        Throw.ArgumentOutOfRangeException.IfValueLessThan(defaultWriteBufferSize, 0, nameof(defaultWriteBufferSize));
        Throw.ArgumentOutOfRangeException.IfValueLessThan(maxWriteBufferSize, 0, nameof(maxWriteBufferSize));
    }
}
