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
using System.Collections.Generic;
using Neo4j.Driver.Preview.Encryption;

namespace Neo4j.Driver.Internal.Encryption;

internal class EnvelopeMetadataExtractor : IEnvelopeMetadataExtractor
{
    private static readonly long DefaultAadEncodingSchemeMajor = BoltValueSerializationSchemeVersion.Latest.Major;
    private static readonly long DefaultAadEncodingSchemeMinor = BoltValueSerializationSchemeVersion.Latest.Minor;

    public EnvelopeMetadata Extract(IDictionary<string, object> metadata)
    {
        var keyId = metadata.GetMandatoryValue<string>(EnvelopeMetadataKeys.KeyId, ExtractionError);
        var iv = metadata.GetMandatoryValue<byte[]>(EnvelopeMetadataKeys.Iv, ExtractionError);
        var aad = metadata.GetOptionalValue<byte[]>(EnvelopeMetadataKeys.Aad, [], ExtractionError);
        var aadEncodingSchemeMajor = GetOptionalInt(
            metadata,
            EnvelopeMetadataKeys.AadEncodingSchemeMajor,
            DefaultAadEncodingSchemeMajor);

        var aadEncodingSchemeMinor = GetOptionalInt(
            metadata,
            EnvelopeMetadataKeys.AadEncodingSchemeMinor,
            DefaultAadEncodingSchemeMinor);

        return new EnvelopeMetadata(keyId, iv, aad, aadEncodingSchemeMajor, aadEncodingSchemeMinor);
    }

    private static int GetOptionalInt(IDictionary<string, object> metadata, string key, long defaultValue)
    {
        var value = metadata.GetOptionalValue(key, defaultValue, ExtractionError);
        if (value is < int.MinValue or > int.MaxValue)
        {
            throw ExtractionError($"Expected key '{key}' to fit in a 32-bit integer, but was {value}.");
        }

        return (int)value;
    }

    private static Exception ExtractionError(string message)
    {
        return new MetadataExtractionException(message);
    }
}

internal record EnvelopeMetadata(
    string KeyId,
    byte[] Iv,
    byte[] Aad,
    int AadEncodingSchemeMajor,
    int AadEncodingSchemeMinor);
