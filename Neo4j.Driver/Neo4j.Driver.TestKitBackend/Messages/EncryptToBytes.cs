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

using Neo4j.Driver.Preview.Encryption;
using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Cypher;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Errors;
using Neo4j.Driver.TestKitBackend.PropertyEncryption;
using Neo4j.Driver.TestKitBackend.Serialization;
using Neo4j.Driver.TestKitBackend.Types;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal record EncryptToBytesRequest : IProtocolMessage
{
    [StoredObject]
    public required IDriver Driver { get; init; }

    public required ICypherValue Value { get; init; }
    public ICypherValue? Aad { get; init; }
    public string? ProfileName { get; init; }
    public string? KeyAlias { get; init; }
    public string? KeyId { get; init; }
    public HexBytes? Iv { get; init; }
}

internal record EncryptedValueResponse(HexBytes EncryptedBytes) : IProtocolMessage;

internal class EncryptToBytesHandler : MessageHandler<EncryptToBytesRequest>
{
    private readonly ICypherToNativeMapper _cypherToNativeMapper;
    private readonly IDriverEncryptionObjectStore _driverEncryptionObjectStore;
    private readonly IResponseWriter _responseWriter;

    public EncryptToBytesHandler(
        ICypherToNativeMapper cypherToNativeMapper,
        IDriverEncryptionObjectStore driverEncryptionObjectStore,
        IResponseWriter responseWriter)
    {
        _cypherToNativeMapper = cypherToNativeMapper;
        _driverEncryptionObjectStore = driverEncryptionObjectStore;
        _responseWriter = responseWriter;
    }

    public override async Task ProcessAsync(EncryptToBytesRequest message)
    {
        var hasKeyAlias = message.KeyAlias is not null;
        var hasKeyId = message.KeyId is not null;

        if (hasKeyAlias == hasKeyId)
        {
            throw new FrontendException("Exactly one of keyAlias or keyId must be set.");
        }

        if (message.Iv is { } iv)
        {
            _driverEncryptionObjectStore.GetIvProvider(message.Driver).SetNextIv(iv);
        }

        var keyStep = message.Driver
            .PropertyEncryption()
            .EncryptRequest()
            .FromValue(_cypherToNativeMapper.Map(message.Value)!);

        if (message.Aad is not null)
        {
            keyStep = keyStep.WithAad(_cypherToNativeMapper.Map(message.Aad)!);
        }

        if (message.ProfileName is not null)
        {
            keyStep = keyStep.UsingProfile(message.ProfileName);
        }

        var executeStep = message.KeyAlias is not null
            ? keyStep.UsingKeyAlias(message.KeyAlias)
            : keyStep.UsingKeyId(message.KeyId!);

        var encryptedBytes = await executeStep.EncryptToBytesAsync();

        await _responseWriter.WriteAsync(new EncryptedValueResponse(encryptedBytes));
    }
}
