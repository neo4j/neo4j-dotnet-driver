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

namespace Neo4j.Driver.Internal
{
    internal static class Throw
    {
        public static class ObjectDisposedException
        {
            public static void FailedToAcquireConnectionDueToPoolClosed(object obj)
            {
                throw new System.ObjectDisposedException(obj.GetType().Name, $"Failed to acquire a new connection as the driver has already been disposed.");
            }


            public static void FailedToAcquireConnection(object obj)
            {
                throw new System.ObjectDisposedException(obj.GetType().Name, $"Failed to acquire a new connection as the driver has already been disposed.");
            }
        }

        public static class ArgumentNullException
        {
            public static void IfNull(object parameter, string paramName)
            {
                If(() => parameter == null, paramName);
            }

            public static void If(Func<bool> func, string paramName)
            {
                if (func())
                {
                    throw new System.ArgumentNullException(paramName);
                }
            }
        }

        public static class ProtocolException
        {
            public static void IfNotEqual(int first, int second, string firstParam, string secondParam)
            {
                If(() => first != second, first, second, firstParam, secondParam);
            }

            internal static void IfNotEqual(object first, object second, string firstParam, string secondParam)
            {
                if (first == null && second == null)
                    return;

                If( () => first == null || second == null || !first.Equals(second), first, second, firstParam,secondParam);
            }

            public static void If(Func<bool> func, object first, object second, string firstParam, string secondParam)
            {
                if(func())
                    throw new V1.ProtocolException($"{firstParam} ({first}) does not equal to {secondParam} ({second})");
            }

            public static void IfFalse(bool value, string nameofValue)
            {
                if(!value)
                    throw new V1.ProtocolException($"Expecting {nameofValue} to be true, however the value is false");
            }
        }

        public static class ArgumentOutOfRangeException
        {
            public static void IfValueLessThan(long value, long limit, string parameterName)
            {
                if(value < limit)
                    throw new System.ArgumentOutOfRangeException(parameterName, value, $"Value given ({value}) cannot be less than {limit}.");
            }

            public static void IfValueGreaterThan(long value, long limit, string parameterName)
            {
                if(value > limit)
                    throw new System.ArgumentOutOfRangeException(parameterName, value, $"Value given ({value}) cannot be greater than {limit}.");
            }
            public static void IfFalse(bool value, string nameofValue)
            {
                if (!value)
                    throw new System.ArgumentOutOfRangeException($"Expecting {nameofValue} to be true, however the value is false");
            }

            public static void IfValueNotBetween(long value, long minInclusive, long maxInclusive, string parameterName)
            {
                if (value < minInclusive || value > maxInclusive)
                    throw new System.ArgumentOutOfRangeException(parameterName, value, $"Value given ({value}) must be between {minInclusive} and {maxInclusive}.");
            }

        }
    }
}
