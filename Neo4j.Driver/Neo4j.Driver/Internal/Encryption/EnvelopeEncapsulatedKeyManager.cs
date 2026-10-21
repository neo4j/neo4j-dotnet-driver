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

internal class EnvelopeEncapsulatedKeyManager : IEncapsulatedKeyManager
{
    private static readonly IKeyEncapsulationOptions EmptyOptions =
        new MapKeyEncapsulationOptions(new Dictionary<string, string>());

    private readonly IKeyEncapsulationService _keyEncapsulationService;
    private readonly IEncapsulatedKeyRecordRepository _keyRepository;
    private readonly IEncryptionErrorPolicy _errorPolicy;

    public EnvelopeEncapsulatedKeyManager(
        IKeyEncapsulationService keyEncapsulationService,
        IEncapsulatedKeyRecordRepository keyRepository,
        IEncryptionErrorPolicy errorPolicy)
    {
        _keyEncapsulationService = keyEncapsulationService;
        _keyRepository = keyRepository;
        _errorPolicy = errorPolicy;
    }

    public async Task<EncapsulatedKey> CreateAsync(
        string? alias = null,
        IKeyEncapsulationOptions? encapsulationOptions = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _keyEncapsulationService
                .EncapsulateAsync(encapsulationOptions ?? EmptyOptions, cancellationToken)
                .ConfigureAwait(false);

            return await _keyRepository
                .CreateAsync(alias, result.Encapsulation, result.Metadata, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _errorPolicy.Throw("key creation", e, cancellationToken);
            throw;
        }
    }

    public async Task<EncapsulatedKey?> FindByAliasAsync(string alias, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _keyRepository.FindByAliasAsync(alias, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _errorPolicy.Throw("key lookup", e, cancellationToken);
            throw;
        }
    }

    public async Task SetAliasByIdAsync(string id, string? alias, CancellationToken cancellationToken = default)
    {
        try
        {
            await _keyRepository.SetAliasByIdAsync(id, alias, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _errorPolicy.Throw("alias update", e, cancellationToken);
            throw;
        }
    }

    public Task DeleteAliasByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return SetAliasByIdAsync(id, null, cancellationToken);
    }

    public async Task DeleteByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _keyRepository.DeleteByIdAsync(id, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _errorPolicy.Throw("key deletion", e, cancellationToken);
            throw;
        }
    }
}
