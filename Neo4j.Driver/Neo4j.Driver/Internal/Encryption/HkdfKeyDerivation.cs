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

using System.Security.Cryptography;
using System.Text;

namespace Neo4j.Driver.Internal.Encryption;

[DriverAutoRegister(singleton: true)]
internal class HkdfKeyDerivation : IKeyDerivation
{
    private const string InfoString = "neo4j/property-encryption/v1";

    public byte[] Derive(byte[] ikm, int outputLength)
    {
        var info = Encoding.UTF8.GetBytes(InfoString);
        return Derive(ikm, null, info, outputLength);
    }

    internal byte[] Derive(byte[] ikm, byte[]? salt, byte[] info, int outputLength)
    {
        return HKDF.DeriveKey(HashAlgorithmName.SHA256, ikm, outputLength, salt, info);
    }
}
