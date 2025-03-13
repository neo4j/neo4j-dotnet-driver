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

namespace Neo4j.Driver.Internal.HomeDbCaching;

internal readonly struct HomeDbCacheKey(object key) : IEquatable<HomeDbCacheKey>
{
    private readonly object _key = key;

    public override int GetHashCode() => _key.GetHashCode();

    public override string ToString() => $"{_key.ToString()} ({GetHashCode():x8})";

    public override bool Equals(object obj)
    {
        return obj is HomeDbCacheKey other && _key.Equals(other._key);
    }

    public static readonly HomeDbCacheKey Default = new ("default");

    /// <inheritdoc />
    public bool Equals(HomeDbCacheKey other)
    {
        return Equals(_key, other._key);
    }
}
