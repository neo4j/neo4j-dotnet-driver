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
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Preview.Encryption;

namespace Neo4j.Driver.Internal.Encryption;

[DriverAutoRegister(singleton: true)]
internal class EncryptionEngineDispatcher : IEncryptionEngineDispatcher
{
    private readonly IEnumerable<IEncryptionEngine> _engines;
    private readonly IEncryptionErrorPolicy _errorPolicy;

    public EncryptionEngineDispatcher(IEnumerable<IEncryptionEngine> engines, IEncryptionErrorPolicy errorPolicy)
    {
        _engines = engines;
        _errorPolicy = errorPolicy;
    }

    public async Task<byte[]> DispatchEncryptAsync(
        IInternalEncryptionProfile profile,
        object value,
        KeyReference keyRef,
        byte[]? aad,
        byte[]? iv,
        CancellationToken cancellationToken)
    {
        try
        {
            foreach (var engine in _engines)
            {
                if (engine.TryStartEncrypt(profile, value, keyRef, aad, iv, cancellationToken, out var task))
                {
                    return await task.ConfigureAwait(false);
                }
            }

            throw new EncryptionEngineNotFoundException(profile.Name);
        }
        catch (Exception e)
        {
            _errorPolicy.Throw("encryption", e);
            throw;
        }
    }

    public async Task<object> DispatchDecryptAsync(
        IInternalEncryptionProfile profile,
        byte[] encrypted,
        byte[]? aad,
        CancellationToken cancellationToken)
    {
        try
        {
            foreach (var engine in _engines)
            {
                if (engine.TryStartDecrypt(profile, encrypted, aad, cancellationToken, out var task))
                {
                    return await task.ConfigureAwait(false);
                }
            }

            throw new EncryptionEngineNotFoundException(profile.Name);
        }
        catch (Exception e)
        {
            _errorPolicy.Throw("decryption", e);
            throw;
        }
    }
}
