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

internal class DecryptRequestBuilder :
    IDecryptRequestValueStep,
    IDecryptRequestAadStep,
    IDecryptRequestExecuteStep
{
    private readonly IEncryptionRequestRunner _runner;
    private byte[]? _value;
    private object? _aad;
    private bool _usePersistedAad;

    public DecryptRequestBuilder(IEncryptionRequestRunner runner)
    {
        _runner = runner;
    }

    public IDecryptRequestAadStep FromValue(byte[] value)
    {
        _value = value;
        return this;
    }

    public IDecryptRequestExecuteStep WithAad(object aad)
    {
        _aad = aad;
        _usePersistedAad = false;
        return this;
    }

    public IDecryptRequestExecuteStep WithPersistedAad()
    {
        _aad = null;
        _usePersistedAad = true;
        return this;
    }

    public Task<object> DecryptAsync(CancellationToken cancellationToken = default)
    {
        var request = new DecryptRequest(_value!, _aad, _usePersistedAad);
        return _runner.DecryptAsync(request, cancellationToken);
    }
}
