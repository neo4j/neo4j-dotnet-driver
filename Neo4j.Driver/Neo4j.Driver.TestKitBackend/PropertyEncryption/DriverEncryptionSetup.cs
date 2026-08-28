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

internal record PropertyEncryptionProfileInput(string Name, HexBytes? Kek = null);

internal record DriverEncryptionObjects(
    IFixedIvProvider IvProvider,
    IReadOnlyList<IPropertyEncryptionProfile> Profiles,
    IReadOnlyDictionary<string, ITestkitEncapsulatedKeyRepository> RepositoriesByProfileName);

internal interface IDriverEncryptionSetup
{
    DriverEncryptionObjects Prepare(IReadOnlyList<PropertyEncryptionProfileInput> profiles);
}

internal class DriverEncryptionSetup : IDriverEncryptionSetup
{
    private readonly Func<byte[]?, IKeyEncapsulationService> _keyEncapsulationServiceFactory;
    private readonly Func<ITestkitEncapsulatedKeyRepository> _repositoryFactory;
    private readonly Func<IFixedIvProvider> _ivProviderFactory;

    public DriverEncryptionSetup(
        Func<byte[]?, IKeyEncapsulationService> keyEncapsulationServiceFactory,
        Func<ITestkitEncapsulatedKeyRepository> repositoryFactory,
        Func<IFixedIvProvider> ivProviderFactory)
    {
        _keyEncapsulationServiceFactory = keyEncapsulationServiceFactory;
        _repositoryFactory = repositoryFactory;
        _ivProviderFactory = ivProviderFactory;
    }

    public DriverEncryptionObjects Prepare(IReadOnlyList<PropertyEncryptionProfileInput> profiles)
    {
        var ivProvider = _ivProviderFactory();
        var repositories = new Dictionary<string, ITestkitEncapsulatedKeyRepository>();
        var resultProfiles = new List<IPropertyEncryptionProfile>();

        foreach (var profile in profiles)
        {
            var keyEncapsulationService = _keyEncapsulationServiceFactory(profile.Kek);
            var repository = _repositoryFactory();

            repositories[profile.Name] = repository;
            resultProfiles.Add(PropertyEncryptionProfile.Envelope(profile.Name, keyEncapsulationService, repository));
        }

        return new DriverEncryptionObjects(ivProvider, resultProfiles, repositories);
    }
}
