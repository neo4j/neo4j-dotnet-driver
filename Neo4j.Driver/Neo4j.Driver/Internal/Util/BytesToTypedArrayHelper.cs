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
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Neo4j.Driver.Internal.Util;

internal class BytesToTypedArrayHelper
{
    private static readonly ConcurrentDictionary<Type, Func<byte[], Array>> Converters = new();

    public static Array ConvertBytesToTypedArray(byte[] bytes, Type elementType)
    {
        // Deal with endianness
        if (BitConverter.IsLittleEndian)
        {
            var elementSize = Marshal.SizeOf(elementType);
            for (var i = 0; i < bytes.Length; i += elementSize)
            {
                Array.Reverse(bytes, i, elementSize);
            }
        }

        var converter = Converters.GetOrAdd(elementType, CreateConverter);
        return converter(bytes);
    }

    private static Array CreateTypedArrayFromBytes<T>(byte[] bytes) where T : unmanaged
    {
        var span = bytes.AsSpan();
        var typedSpan = MemoryMarshal.Cast<byte, T>(span);
        return typedSpan.ToArray();
    }

    private static Func<byte[], Array> CreateConverter(Type elementType)
    {
        var method = typeof(BytesToTypedArrayHelper).GetMethod(
                nameof(CreateTypedArrayFromBytes),
                BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(elementType);

        return bytes => (Array)method.Invoke(null, [bytes])!;
    }
}
