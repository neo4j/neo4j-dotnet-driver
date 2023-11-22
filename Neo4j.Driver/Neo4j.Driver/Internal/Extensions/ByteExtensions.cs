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

namespace Neo4j.Driver.Internal;

internal static class ByteExtensions
{
    public static string ToHexString(this byte[] bytes, string separator, int start = 0, int size = -1)
    {
        if (bytes == null)
        {
            return "NULL";
        }

        if (size < 0)
        {
            size = bytes.Length;
        }

        var destination = new byte[size];
        Array.Copy(bytes, start, destination, 0, size);
        var output = BitConverter.ToString(destination);

        return output.Replace("-", separator);
    }

    public static string ToHexString(this byte[] bytes, int start = 0, int size = -1)
    {
        return bytes.ToHexString(" ", start, size);
    }
}
