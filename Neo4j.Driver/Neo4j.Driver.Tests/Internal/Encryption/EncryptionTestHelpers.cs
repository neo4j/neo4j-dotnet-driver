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

#nullable enable

using System;
using System.Linq;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Encryption;

namespace Neo4j.Driver.Tests.Internal.Encryption;

internal static class EncryptionTestHelpers
{
    public static byte[] Matches(byte[] expected)
    {
        return It.Is<byte[]>(actual => actual.SequenceEqual(expected));
    }

    public static byte[] Sequence(byte length, byte seed = 0)
    {
        return Enumerable.TypedRange(seed, length).ToArray();
    }
}

internal class SequentialRandom : ICryptoRandomProvider
{
    public void Fill(Span<byte> buffer)
    {
        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] = (byte)i;
        }
    }
}
