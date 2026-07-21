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

// Finds the IEncryptionEngine that accepts the given profile (via TryStartEncrypt/
// TryStartDecrypt's pattern-match) and awaits its result. Throws
// EncryptionEngineNotFoundException if no registered engine accepts the profile.
internal interface IEncryptionEngineDispatcher
{
    Task<byte[]> DispatchEncryptAsync(
        IInternalEncryptionProfile profile,
        object value,
        KeyReference keyRef,
        byte[]? aad,
        CancellationToken cancellationToken);

    Task<object> DispatchDecryptAsync(
        IInternalEncryptionProfile profile,
        byte[] encrypted,
        byte[]? aad,
        CancellationToken cancellationToken);
}
