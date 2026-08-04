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

using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal.Encryption;

[DriverAutoRegister(singleton: true)]
internal class EncryptionRequestRunner : IEncryptionRequestRunner
{
    private readonly IEncryptionProfileRegistry _registry;
    private readonly IEncryptionEngineDispatcher _dispatcher;
    private readonly IPlaintextCodec _plaintextCodec;
    private readonly IEncryptedValueBytesCodec _encryptedValueBytesCodec;

    public EncryptionRequestRunner(
        IEncryptionProfileRegistry registry,
        IEncryptionEngineDispatcher dispatcher,
        IPlaintextCodec plaintextCodec,
        IEncryptedValueBytesCodec encryptedValueBytesCodec)
    {
        _registry = registry;
        _dispatcher = dispatcher;
        _plaintextCodec = plaintextCodec;
        _encryptedValueBytesCodec = encryptedValueBytesCodec;
    }

    public Task<byte[]> EncryptToBytesAsync(EncryptRequest request, CancellationToken cancellationToken)
    {
        var profile = _registry.Get(request.ProfileName);
        var aad = request.Aad is null ? null : _plaintextCodec.Serialize(request.Aad);
        return _dispatcher.DispatchEncryptAsync(profile, request.Value, request.KeyReference, aad, cancellationToken);
    }

    public Task<object> DecryptAsync(DecryptRequest request, CancellationToken cancellationToken)
    {
        var profileName = _encryptedValueBytesCodec.PeekProfileName(request.Value);
        var profile = _registry.Get(profileName);
        var aad = request.UsePersistedAad ? null : _plaintextCodec.Serialize(request.Aad!);
        return _dispatcher.DispatchDecryptAsync(profile, request.Value, aad, cancellationToken);
    }
}
