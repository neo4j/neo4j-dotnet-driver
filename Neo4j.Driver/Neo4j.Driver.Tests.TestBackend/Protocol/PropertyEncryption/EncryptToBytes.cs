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
using System.Threading.Tasks;
using Neo4j.Driver.Preview.Encryption;
using Neo4j.Driver.Tests.TestBackend.Protocol.Driver;
using Neo4j.Driver.Tests.TestBackend.Protocol.JsonConverters;
using Neo4j.Driver.Tests.TestBackend.Types;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend.Protocol.PropertyEncryption;

internal class EncryptToBytes : ProtocolObject
{
    public EncryptToBytesType data { get; set; } = new();

    [JsonIgnore]
    private byte[] EncryptedBytes { get; set; }

    public override async Task Process()
    {
        var newDriver = ObjManager.GetObject<NewDriver>(data.driverId);
        var driver = newDriver.Driver;
        var value = CypherToNative.Convert(data.value);

        if (data.fixedIv != null)
        {
            newDriver.FixedIvProvider.SetNextIv((byte[])CypherToNative.Convert(data.fixedIv));
        }

        IEncryptRequestKeyStep keyStep = driver.PropertyEncryption().EncryptRequest().FromValue(value);

        if (data.aad != null)
        {
            keyStep = keyStep.WithAad(CypherToNative.Convert(data.aad));
        }

        if (data.profileName != null)
        {
            keyStep = keyStep.UsingProfile(data.profileName);
        }

        if (data.keyAlias == null && data.keyId == null)
        {
            throw new ArgumentException("Either keyAlias or keyId must be set.");
        }

        if (data.keyAlias != null && data.keyId != null)
        {
            throw new ArgumentException("Only one of keyAlias or keyId may be set.");
        }

        var executeStep = data.keyAlias != null
            ? keyStep.UsingKeyAlias(data.keyAlias)
            : keyStep.UsingKeyId(data.keyId);

        EncryptedBytes = await executeStep.EncryptToBytesAsync();

        newDriver.FixedIvProvider.EnsureConsumed();
    }

    public override string Respond()
    {
        return new ProtocolResponse("EncryptedValue", new { encryptedBytes = NativeToCypher.Convert(EncryptedBytes) })
            .Encode();
    }

    public class EncryptToBytesType
    {
        public string driverId { get; set; }

        [JsonConverter(typeof(SingleCypherValueConverter))]
        public CypherToNativeObject value { get; set; }

        [JsonConverter(typeof(SingleCypherValueConverter))]
        public CypherToNativeObject aad { get; set; }

        public string profileName { get; set; }
        public string keyAlias { get; set; }
        public string keyId { get; set; }

        [JsonConverter(typeof(SingleCypherValueConverter))]
        public CypherToNativeObject fixedIv { get; set; }
    }
}
