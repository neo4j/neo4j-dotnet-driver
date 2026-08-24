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

using System.Collections.Generic;
using System.Threading.Tasks;
using Neo4j.Driver.Preview.Encryption;
using Neo4j.Driver.Tests.TestBackend.Types;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend.Protocol.PropertyEncryption;

internal class ImportEncapsulatedKey : ProtocolObject
{
    public ImportEncapsulatedKeyType data { get; set; } = new();

    [JsonIgnore]
    private EncapsulatedKey Key { get; set; }

    public override Task Process()
    {
        var fixture = ObjManager.GetObject<EncryptionProfileFixture>($"{data.driverId}:{data.profileName}");
        var encapsulation = CypherToNative.ConvertStringToBytes(data.encapsulation);
        Key = fixture.KeyRepository.Import(data.keyId, data.alias, encapsulation, data.metadata);
        return Task.CompletedTask;
    }

    public override string Respond()
    {
        return new ProtocolResponse(
                "EncapsulatedKey",
                new
                {
                    id = Key.Id,
                    alias = Key.Alias,
                    encapsulatedBytes = NativeToCypher.ByteStreamToHexString(Key.Encapsulation),
                    metadata = Key.Metadata
                })
            .Encode();
    }

    public class ImportEncapsulatedKeyType
    {
        public string driverId { get; set; }
        public string keyId { get; set; }
        public string alias { get; set; }

        public string encapsulation { get; set; }

        public IReadOnlyDictionary<string, string> metadata { get; set; }
        public string profileName { get; set; }
    }
}
