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

using System.Security.Cryptography;
using Neo4j.Driver.Internal.Encryption;

namespace Neo4j.Driver.TestKitBackend.PropertyEncryption;

internal interface IFixedIvProvider : IIvProvider
{
    void SetNextIv(ReadOnlySpan<byte> iv);
    void EnsureConsumed();
}

internal class FixedIvProvider : IFixedIvProvider
{
    private const int IvLength = 12;

    private byte[]? _pendingIv;

    public void SetNextIv(ReadOnlySpan<byte> iv)
    {
        if (iv.Length != IvLength)
        {
            throw new ArgumentException($"A fixed IV must be exactly {IvLength} bytes, but was {iv.Length}.");
        }

        if (_pendingIv != null)
        {
            throw new ArgumentException("A fixed IV was set while the previous one is still unconsumed.");
        }

        _pendingIv = iv.ToArray();
    }

    public void EnsureConsumed()
    {
        if (_pendingIv != null)
        {
            throw new ArgumentException("The fixed IV was not consumed by the operation.");
        }
    }

    public byte[] GetIv()
    {
        if (_pendingIv == null)
        {
            return RandomNumberGenerator.GetBytes(IvLength);
        }

        var iv = _pendingIv;
        _pendingIv = null;
        return iv;
    }
}
