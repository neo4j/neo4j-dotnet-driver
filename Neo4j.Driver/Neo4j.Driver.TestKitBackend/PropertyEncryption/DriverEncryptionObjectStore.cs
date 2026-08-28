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

using Neo4j.Driver.TestKitBackend.Serialization;

namespace Neo4j.Driver.TestKitBackend.PropertyEncryption;

[RegistrationLifetime(RegistrationLifetime.PerLifetimeScope)]
internal class DriverEncryptionObjectStore : IDriverEncryptionObjectStore
{
    private readonly Dictionary<IDriver, DriverEncryptionObjects> _objectsByDriver = new();

    public void StoreObjects(IDriver driver, DriverEncryptionObjects objects)
    {
        _objectsByDriver[driver] = objects;
    }

    public IFixedIvProvider GetIvProvider(IDriver driver)
    {
        return GetObjects(driver).IvProvider;
    }

    public ITestkitEncapsulatedKeyRepository GetRepository(IDriver driver, string? profileName = null)
    {
        var repositories = GetObjects(driver).RepositoriesByProfileName;

        if (profileName is null)
        {
            return repositories.Count switch
            {
                1 => repositories.Values.First(),
                0 => throw new TestKitProtocolException("The driver has no property-encryption profiles configured."),
                _ => throw new TestKitProtocolException(
                    "Multiple property-encryption profiles are configured; a profile name must be specified.")
            };
        }

        if (!repositories.TryGetValue(profileName, out var repository))
        {
            throw new TestKitProtocolException(
                $"The driver has no property-encryption profile named '{profileName}'.");
        }

        return repository;
    }

    private DriverEncryptionObjects GetObjects(IDriver driver)
    {
        return _objectsByDriver.TryGetValue(driver, out var objects)
            ? objects
            : throw new TestKitProtocolException("The driver was created without property-encryption profiles.");
    }
}
