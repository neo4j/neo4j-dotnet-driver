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

using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.PropertyEncryption;
using Neo4j.Driver.TestKitBackend.Serialization;
using Neo4j.Driver.TestKitBackend.Types;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal record ImportEncapsulatedKeyRequest : IProtocolMessage
{
    [StoredObject]
    public required IDriver Driver { get; init; }

    public required string KeyId { get; init; }
    public required string Alias { get; init; }
    public required HexBytes Encapsulation { get; init; }
    public required IReadOnlyDictionary<string, string> Metadata { get; init; }
    public string? ProfileName { get; init; }
}

internal class ImportEncapsulatedKeyHandler : MessageHandler<ImportEncapsulatedKeyRequest>
{
    private readonly IDriverEncryptionObjectStore _driverEncryptionObjectStore;
    private readonly IResponseWriter _responseWriter;

    public ImportEncapsulatedKeyHandler(
        IDriverEncryptionObjectStore driverEncryptionObjectStore,
        IResponseWriter responseWriter)
    {
        _driverEncryptionObjectStore = driverEncryptionObjectStore;
        _responseWriter = responseWriter;
    }

    public override async Task ProcessAsync(ImportEncapsulatedKeyRequest message)
    {
        var key = _driverEncryptionObjectStore
            .GetRepository(message.Driver, message.ProfileName)
            .Import(message.KeyId, message.Alias, message.Encapsulation, message.Metadata);

        await _responseWriter.WriteAsync(new EncapsulatedKeyResponse(key.Id, key.Alias));
    }
}
