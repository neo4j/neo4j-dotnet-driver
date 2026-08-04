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

internal interface IEncryptionRequestRunner
{
    Task<byte[]> EncryptToBytesAsync(EncryptRequest request, CancellationToken cancellationToken);
    Task<object> DecryptAsync(DecryptRequest request, CancellationToken cancellationToken);
}

internal record EncryptRequest(object Value, object? Aad, string? ProfileName, KeyReference KeyReference);

internal record DecryptRequest(byte[] Value, object? Aad, bool UsePersistedAad);
