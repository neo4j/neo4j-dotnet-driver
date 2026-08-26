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

internal record PropertyEncryptionProfileInput(string Name, string? Kek);

internal record DriverEncryptionSetupResult(
    IFixedIvProvider IvProvider,
    IReadOnlyList<IPropertyEncryptionProfile> Profiles,
    IReadOnlyDictionary<string, ITestkitEncapsulatedKeyRepository> RepositoriesByProfileName);

internal interface IDriverEncryptionSetup
{
    DriverEncryptionSetupResult Prepare(IReadOnlyList<PropertyEncryptionProfileInput> profiles);
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

    public DriverEncryptionSetupResult Prepare(IReadOnlyList<PropertyEncryptionProfileInput> profiles)
    {
        var ivProvider = _ivProviderFactory();
        var repositoriesByProfileName = new Dictionary<string, ITestkitEncapsulatedKeyRepository>();
        var resultProfiles = new List<IPropertyEncryptionProfile>();

        foreach (var profile in profiles)
        {
            var kek = profile.Kek is null ? null : Convert.FromHexString(profile.Kek.Replace(" ", ""));
            var keyEncapsulationService = _keyEncapsulationServiceFactory(kek);
            var repository = _repositoryFactory();

            repositoriesByProfileName[profile.Name] = repository;
            resultProfiles.Add(PropertyEncryptionProfile.Envelope(profile.Name, keyEncapsulationService, repository));
        }

        return new DriverEncryptionSetupResult(ivProvider, resultProfiles, repositoriesByProfileName);
    }
}
