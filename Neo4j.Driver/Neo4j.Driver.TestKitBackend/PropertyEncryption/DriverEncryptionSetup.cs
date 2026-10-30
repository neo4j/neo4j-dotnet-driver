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
using Neo4j.Driver.TestKitBackend.Types;

namespace Neo4j.Driver.TestKitBackend.PropertyEncryption;

internal record PropertyEncryptionProfileInput(string Name, string RepositoryId, HexBytes? Kek = null);

internal interface IDriverEncryptionSetup
{
    IReadOnlyList<IPropertyEncryptionProfile> Prepare(IReadOnlyList<PropertyEncryptionProfileInput> profiles);
}

internal class DriverEncryptionSetup : IDriverEncryptionSetup
{
    private readonly Func<byte[]?, IKeyEncapsulationService> _keyEncapsulationServiceFactory;
    private readonly Func<string, IEncapsulatedKeyRecordRepository> _repositoryFactory;

    public DriverEncryptionSetup(
        Func<byte[]?, IKeyEncapsulationService> keyEncapsulationServiceFactory,
        Func<string, IEncapsulatedKeyRecordRepository> repositoryFactory)
    {
        _keyEncapsulationServiceFactory = keyEncapsulationServiceFactory;
        _repositoryFactory = repositoryFactory;
    }

    public IReadOnlyList<IPropertyEncryptionProfile> Prepare(IReadOnlyList<PropertyEncryptionProfileInput> profiles)
    {
        return profiles
            .Select(profile => PropertyEncryptionProfile
                .EnvelopeBuilder(
                    profile.Name,
                    _keyEncapsulationServiceFactory(profile.Kek),
                    _repositoryFactory(profile.RepositoryId))
                .Build())
            .ToList();
    }
}
