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
using System.Linq;
using Neo4j.Driver.Internal.Encryption;

namespace Neo4j.Driver.Tests.TestBackend.PropertyEncryption;

internal class MockCryptoRandomProvider : ICryptoRandomProvider, IMockCryptoRandomProvider
{
    private List<byte> _bytes = [];

    public void ProvideBytes(IEnumerable<byte> bytes)
    {
        if (_bytes.Count != 0)
        {
            throw new ArgumentException(
                $"Mock random bytes provided while {_bytes.Count} unconsumed bytes remain.");
        }

        _bytes = [..bytes];
    }

    public void Fill(Span<byte> buffer)
    {
        if (buffer.Length > _bytes.Count)
        {
            throw new ArgumentException(
                $"{buffer.Length} mock random bytes requested but only {_bytes.Count} remain.");
        }

        _bytes[..buffer.Length].CopyTo(buffer);
        _bytes = _bytes[buffer.Length..];
    }

    public void EnsureAllBytesConsumed()
    {
        if (_bytes.Count != 0)
        {
            throw new ArgumentException(
                $"{_bytes.Count} mock random bytes remain unconsumed.");
        }
    }
}
