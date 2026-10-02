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
using Neo4j.Driver.TestKitBackend.Expectations;
using Neo4j.Driver.TestKitBackend.Messages;

namespace Neo4j.Driver.TestKitBackend.PropertyEncryption;

internal class ReverseRequestEncapsulatedKeyRepository : ITestkitEncapsulatedKeyRepository
{
    private readonly IOutboundRoundTrip _roundTrip;
    private readonly string _repositoryId;

    public ReverseRequestEncapsulatedKeyRepository(IOutboundRoundTrip roundTrip)
        : this(roundTrip, Guid.NewGuid().ToString("N"))
    {
    }

    internal ReverseRequestEncapsulatedKeyRepository(IOutboundRoundTrip roundTrip, string repositoryId)
    {
        _roundTrip = roundTrip;
        _repositoryId = repositoryId;
    }

    public async Task<EncapsulatedKeyRecord?> FindByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var wire = await _roundTrip
            .SendExpectingAsync<EncapsulatedKeyRepositoryRecord?>(new EncapsulatedKeyRepositoryFindByIdRequest(_repositoryId, id))
            .ConfigureAwait(false);

        return wire?.ToRecord();
    }

    public async Task<EncapsulatedKeyRecord?> FindByAliasAsync(string alias, CancellationToken cancellationToken = default)
    {
        var wire = await _roundTrip
            .SendExpectingAsync<EncapsulatedKeyRepositoryRecord?>(
                new EncapsulatedKeyRepositoryFindByAliasRequest(_repositoryId, alias))
            .ConfigureAwait(false);

        return wire?.ToRecord();
    }

    public async Task<EncapsulatedKeyRecord> CreateAsync(
        string? alias,
        byte[] encapsulation,
        IReadOnlyDictionary<string, string> metadata,
        CancellationToken cancellationToken = default)
    {
        var wire = await _roundTrip
            .SendExpectingAsync<EncapsulatedKeyRepositoryRecord>(
                new EncapsulatedKeyRepositoryCreateRequest(_repositoryId, alias, encapsulation, metadata))
            .ConfigureAwait(false);

        return wire.ToRecord();
    }

    public async Task SetAliasByIdAsync(string id, string? alias, CancellationToken cancellationToken = default)
    {
        await _roundTrip
            .SendExpectingAsync<bool>(new EncapsulatedKeyRepositorySetAliasRequest(_repositoryId, id, alias))
            .ConfigureAwait(false);
    }

    public async Task DeleteByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        await _roundTrip
            .SendExpectingAsync<bool>(new EncapsulatedKeyRepositoryDeleteRequest(_repositoryId, id))
            .ConfigureAwait(false);
    }

    public async Task<EncapsulatedKeyRecord> ImportAsync(
        string id,
        string alias,
        byte[] encapsulation,
        IReadOnlyDictionary<string, string> metadata)
    {
        var wire = await _roundTrip
            .SendExpectingAsync<EncapsulatedKeyRepositoryRecord>(
                new EncapsulatedKeyRepositoryImportRequest(_repositoryId, id, alias, encapsulation, metadata))
            .ConfigureAwait(false);

        return wire.ToRecord();
    }
}
