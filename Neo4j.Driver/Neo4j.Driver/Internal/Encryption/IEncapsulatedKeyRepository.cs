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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal.Encryption;

internal interface IEncapsulatedKeyRepository
{
    Task<EncapsulatedKey> FindByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<EncapsulatedKey> FindByAliasAsync(string alias, CancellationToken cancellationToken = default);

    Task<EncapsulatedKey> SaveAsync(
        IEnumerable<string> aliases,
        byte[] encapsulation,
        IReadOnlyDictionary<string, string> metadata,
        CancellationToken cancellationToken = default);

    Task AddAliasByIdAsync(string id, string alias, CancellationToken cancellationToken = default);

    Task DeleteAliasByIdAsync(string id, string alias, CancellationToken cancellationToken = default);

    Task DeleteByIdAsync(string id, CancellationToken cancellationToken = default);
}
