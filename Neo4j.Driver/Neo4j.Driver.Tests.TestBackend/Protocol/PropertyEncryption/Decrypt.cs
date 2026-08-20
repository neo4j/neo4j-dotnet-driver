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

internal class Decrypt : ProtocolObject
{
    public DecryptType data { get; set; } = new();

    [JsonIgnore]
    private object DecryptedValue { get; set; }

    public override async Task Process()
    {
        var driver = ObjManager.GetObject<NewDriver>(data.driverId).Driver;
        var value = CypherToNative.ConvertStringToBytes(data.value);

        if (data.aad == null && !data.usePersistedAad)
        {
            throw new ArgumentException("Either aad or usePersistedAad must be set.");
        }

        if (data.aad != null && data.usePersistedAad)
        {
            throw new ArgumentException("Only one of aad or usePersistedAad may be set.");
        }

        var aadStep = driver.PropertyEncryption().DecryptRequest().FromValue(value);

        var executeStep = data.usePersistedAad
            ? aadStep.WithPersistedAad()
            : aadStep.WithAad(CypherToNative.Convert(data.aad));

        DecryptedValue = await executeStep.DecryptAsync();
    }

    public override string Respond()
    {
        return new ProtocolResponse(
                "DecryptedValue",
                new { decryptedValue = NativeToCypher.Convert(DecryptedValue) })
            .Encode();
    }

    public class DecryptType
    {
        public string driverId { get; set; }

        public string value { get; set; }

        [JsonConverter(typeof(SingleCypherValueConverter))]
        public CypherToNativeObject aad { get; set; }

        public bool usePersistedAad { get; set; }
    }
}
