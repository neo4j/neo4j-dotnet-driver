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
using System.Text.RegularExpressions;
using Neo4j.Driver.Internal.Protocol;

namespace Neo4j.Driver.Internal.Encryption;

// keyId + iv are mandatory, aad is optional, any "opt." prefixed keys are KES-specific
// encapsulation options and are not parsed
[DriverAutoRegister(singleton: true)]
internal class EnvelopeMetadataExtractor : IEnvelopeMetadataExtractor
{
    // aad_protocol_* is absent from older/foreign-driver metadata written before this field
    // existed - default to the latest baseline, matching the engine's fixed AAD protocol version.
    private static readonly int DefaultAadProtocolMajor = BoltProtocolVersion.V6_1.MajorVersion;
    private static readonly int DefaultAadProtocolMinor = BoltProtocolVersion.V6_1.MinorVersion;

    public EnvelopeMetadata Extract(IDictionary<string, object> metadata)
    {
        var aad = metadata.GetOptionalValue<byte[]>(EnvelopeMetadataKeys.Aad, [], ExtractionError);
        var options = ExtractEncapsulationOptions(metadata);
        var keyId = metadata.GetMandatoryValue<string>(EnvelopeMetadataKeys.KeyId, m => new MetadataExtractionException(m));
        var iv = metadata.GetMandatoryValue<byte[]>(EnvelopeMetadataKeys.Iv, m => new MetadataExtractionException(m));
        
        var aadProtocolMajor = metadata.GetOptionalValue(
            EnvelopeMetadataKeys.AadProtocolMajor,
            DefaultAadProtocolMajor,
            ExtractionError);

        var aadProtocolMinor = metadata.GetOptionalValue(
            EnvelopeMetadataKeys.AadProtocolMinor,
            DefaultAadProtocolMinor,
            ExtractionError);

        return new EnvelopeMetadata(keyId, iv, aad, aadProtocolMajor, aadProtocolMinor, options);

        static Exception ExtractionError(string message)
        {
            return new MetadataExtractionException(message);
        }
    }

    private static Dictionary<string, object> ExtractEncapsulationOptions(IDictionary<string, object> metadata)
    {
        const string optItemRegex = @"^opt\.(.*)$";

        var options = new Dictionary<string, object>();
        foreach (var (key, value) in metadata)
        {
            var match = Regex.Match(key, optItemRegex);
            if (!match.Success)
            {
                continue;
            }

            var newKey = match.Groups[1].Value;
            options[newKey] = value;
        }

        return options;
    }
}

internal record EnvelopeMetadata(
    string KeyId,
    byte[] Iv,
    byte[] Aad,
    int AadProtocolMajor,
    int AadProtocolMinor,
    IDictionary<string, object> EncapsulationOptions);
