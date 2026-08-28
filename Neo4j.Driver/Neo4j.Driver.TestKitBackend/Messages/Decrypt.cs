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
using Neo4j.Driver.TestKitBackend.Serialization;
using Neo4j.Driver.TestKitBackend.Types;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal record DecryptRequest : IProtocolMessage
{
    [StoredObject]
    public required IDriver Driver { get; init; }

    public required HexBytes Value { get; init; }
    public ICypherValue? Aad { get; init; }
    public bool UsePersistedAad { get; init; }
}

internal record DecryptedValueResponse(ICypherValue DecryptedValue) : IProtocolMessage;

internal class DecryptHandler : MessageHandler<DecryptRequest>
{
    private readonly ICypherToNativeMapper _cypherToNativeMapper;
    private readonly INativeToCypherMapper _nativeToCypherMapper;
    private readonly IResponseWriter _responseWriter;

    public DecryptHandler(
        ICypherToNativeMapper cypherToNativeMapper,
        INativeToCypherMapper nativeToCypherMapper,
        IResponseWriter responseWriter)
    {
        _cypherToNativeMapper = cypherToNativeMapper;
        _nativeToCypherMapper = nativeToCypherMapper;
        _responseWriter = responseWriter;
    }

    public override async Task ProcessAsync(DecryptRequest message)
    {
        var hasExplicitAad = message.Aad is not null;

        if (hasExplicitAad == message.UsePersistedAad)
        {
            throw new FrontendException("Exactly one of aad or usePersistedAad must be set.");
        }

        var aadStep = message.Driver.PropertyEncryption().DecryptRequest().FromValue(message.Value);

        var executeStep = message.UsePersistedAad
            ? aadStep.WithPersistedAad()
            : aadStep.WithAad(_cypherToNativeMapper.Map(message.Aad!)!);

        var decrypted = await executeStep.DecryptAsync();

        await _responseWriter.WriteAsync(new DecryptedValueResponse(_nativeToCypherMapper.Map(decrypted)));
    }
}
