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
using Neo4j.Driver.Preview.Encryption;

namespace Neo4j.Driver.Internal.Encryption;

internal class EncryptRequestBuilder :
    IEncryptRequestValueStep,
    IEncryptRequestKeyStep,
    IEncryptRequestExecuteStep,
    IInternalEncryptRequest
{
    private readonly IEncryptionRequestRunner _runner;
    private object? _value;
    private object? _aad;
    private string? _profileName;
    private KeyReference? _keyReference;
    private byte[]? _iv;

    public EncryptRequestBuilder(IEncryptionRequestRunner runner)
    {
        _runner = runner;
    }

    public IEncryptRequestKeyStep FromValue(object value)
    {
        _value = value;
        return this;
    }

    public IEncryptRequestKeyStep WithAad(object aad)
    {
        _aad = aad;
        return this;
    }

    public IEncryptRequestKeyStep UsingProfile(string profileName)
    {
        _profileName = profileName;
        return this;
    }

    public IEncryptRequestExecuteStep UsingKeyAlias(string alias)
    {
        _keyReference = new KeyReference(alias, KeyReferenceType.Alias);
        return this;
    }

    public IEncryptRequestExecuteStep UsingKeyId(string id)
    {
        _keyReference = new KeyReference(id, KeyReferenceType.Id);
        return this;
    }

    public void UseFixedIv(byte[] iv)
    {
        _iv = iv;
    }

    public Task<byte[]> EncryptToBytesAsync(CancellationToken cancellationToken = default)
    {
        var request = new EncryptRequest(_value!, _aad, _profileName, _keyReference!, _iv);
        return _runner.EncryptToBytesAsync(request, cancellationToken);
    }
}
