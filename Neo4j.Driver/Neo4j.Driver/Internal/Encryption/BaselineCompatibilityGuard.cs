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

using System.Diagnostics.CodeAnalysis;
using Neo4j.Driver.Preview.Encryption;

namespace Neo4j.Driver.Internal.Encryption;

[DriverAutoRegister(singleton: true)]
internal class BaselineCompatibilityGuard : IBaselineCompatibilityGuard
{
    public bool IsUnsupportedBaselineType(
        EncryptedStructure structure,
        [NotNullWhen(true)] out UnsupportedType? unsupported)
    {
        var typeBaseline = new BoltValueSerializationSchemeVersion(
            structure.TypeSerializationSchemeMajor,
            structure.TypeSerializationSchemeMinor);

        if (typeBaseline > BoltValueSerializationSchemeVersion.Latest)
        {
            unsupported = new UnsupportedType(
                structure.TypeName,
                structure.TypeSerializationSchemeMajor,
                structure.TypeSerializationSchemeMinor,
                null);

            return true;
        }

        unsupported = null;
        return false;
    }

    public void EnsureAadEncodingSchemeCompatibility(byte[]? suppliedAad, EnvelopeMetadata metadata)
    {
        if (suppliedAad == null)
        {
            return;
        }

        var aadBaseline = new BoltValueSerializationSchemeVersion(metadata.AadEncodingSchemeMajor, metadata.AadEncodingSchemeMinor);
        if (aadBaseline > BoltValueSerializationSchemeVersion.Latest)
        {
            throw new PropertyEncryptionException(
                $"Cannot reproduce AAD bytes: recorded AAD protocol version {aadBaseline} is newer than " +
                $"the maximum supported version {BoltValueSerializationSchemeVersion.Latest}.");
        }
    }
}
