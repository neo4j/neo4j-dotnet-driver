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
using System.Linq;
using Neo4j.Driver.Preview.Encryption;

namespace Neo4j.Driver.Internal.Encryption;

[DriverAutoRegister(singleton: true)]
internal class EncryptionProfileRegistry : IEncryptionProfileRegistry
{
    private readonly Dictionary<string, IInternalEncryptionProfile> _profilesByName = new();

    public EncryptionProfileRegistry(IEnumerable<IInternalEncryptionProfile> profiles)
    {
        foreach (var profile in profiles)
        {
            if (!_profilesByName.TryAdd(profile.Name, profile))
            {
                throw new ArgumentException(
                    $"Duplicate encryption profile name '{profile.Name}'.",
                    nameof(profiles));
            }
        }
    }

    public IInternalEncryptionProfile Get(string? name)
    {
        if (name is null)
        {
            return _profilesByName.Count switch
            {
                1 => _profilesByName.Values.First(),
                0 => throw new DefaultEncryptionProfileNotFoundException(),
                _ => throw new AmbiguousEncryptionProfileException(
                    "Multiple encryption profiles are configured; a profile name must be specified.")
            };
        }

        return _profilesByName.TryGetValue(name, out var profile)
            ? profile
            : throw new EncryptionProfileNotFoundException(name);
    }
}
