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

namespace Neo4j.Driver.TestKitBackend.PropertyEncryption;

internal interface ITestkitEncapsulatedKeyRepository : IEncapsulatedKeyRecordRepository
{
    /// <summary>
    /// This repository's id on the wire, so <c>DriverCloseHandler</c> can tell the testkit frontend
    /// which repositories to forget when the owning driver closes.
    /// </summary>
    string RepositoryId { get; }

    Task<EncapsulatedKeyRecord> ImportAsync(
        string id,
        string alias,
        byte[] encapsulation,
        IReadOnlyDictionary<string, string> metadata);
}
