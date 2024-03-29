﻿// Copyright (c) "Neo4j"
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

namespace Neo4j.Driver.Internal.Helpers;

internal static class Throw
{
    public static class ProtocolException
    {
        public static void IfFalse(bool value, string nameofValue)
        {
            if (!value)
            {
                throw new Neo4j.Driver.ProtocolException(
                    $"Expecting {nameofValue} to be true, however the value is false");
            }
        }
    }

    public static class ArgumentOutOfRangeException
    {
        public static void IfFalse(bool value, string nameofValue)
        {
            if (!value)
            {
                throw new System.ArgumentOutOfRangeException(
                    $"Expecting {nameofValue} to be true, however the value is false");
            }
        }

        public static void IfValueNotBetween(long value, long minInclusive, long maxInclusive, string parameterName)
        {
            if (value < minInclusive || value > maxInclusive)
            {
                throw new System.ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    $"Value given ({value}) must be between {minInclusive} and {maxInclusive}.");
            }
        }
    }
}
