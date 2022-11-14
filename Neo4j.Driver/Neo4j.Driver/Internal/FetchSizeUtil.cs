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

namespace Neo4j.Driver.Internal;

/// <summary>A util class for handling fetch size.</summary>
internal static class FetchSizeUtil
{
    /// <summary>Validate the fetch size. A valid fetch size can be a positive number, or -1.</summary>
    /// <param name="size">The fetch size.</param>
    /// <returns>A valid fetch size.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If the input value is null, 0, or unsupported negative numbers.</exception>
    public static long AssertValidFetchSize(long? size)
    {
        if (!size.HasValue || (size <= 0 && size != Config.Infinite))
        {
            throw new ArgumentOutOfRangeException(
                $"The record fetch size may not be null, 0 or negative. Illegal record fetch size: {size}.");
        }

        return size.Value;
    }
}
