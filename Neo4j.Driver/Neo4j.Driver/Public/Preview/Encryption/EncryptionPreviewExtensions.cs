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

using System;
using System.Collections.Generic;
using Neo4j.Driver.Internal;

namespace Neo4j.Driver.Preview.Encryption;

/// <summary>
/// Extension methods that attach the client-side property encryption API to
/// <see cref="IDriver"/>, <see cref="Config"/>, and <see cref="ConfigBuilder"/>. This class is part
/// of the Encryption Preview feature, and is subject to change or removal.
/// </summary>
public static class EncryptionPreviewExtensions
{
    extension(IDriver driver)
    {
        /// <summary>
        /// Gets the client-side property encryption entry point for this driver. Use it to encrypt
        /// and decrypt property values and to manage encapsulated keys. This method is part of the
        /// Encryption Preview feature, and is subject to change or removal.
        /// </summary>
        /// <returns>The <see cref="IPropertyEncryption"/> entry point for this driver.</returns>
        public IPropertyEncryption PropertyEncryption()
        {
            return ((IInternalDriver)driver).PropertyEncryption();
        }
    }

    extension(Config config)
    {
        /// <summary>
        /// Gets the list of property encryption profiles configured for the Neo4j driver.
        /// This property provides access to the encryption profiles that define how properties
        /// are encrypted and decrypted when interacting with the Neo4j database. Each profile specifies
        /// the encryption algorithms, key management strategies, and other relevant settings for property encryption.
        /// This property is part of the Encryption Preview feature, and is subject to change or removal.
        /// </summary>
        /// <value>A read-only list of property encryption profiles.</value>
        public IReadOnlyList<IPropertyEncryptionProfile> PropertyEncryptionProfiles =>
            config.Preview_PropertyEncryptionProfiles;
    }

    extension(ConfigBuilder configBuilder)
    {
        /// <summary>
        /// Configures the Neo4j driver with encryption profiles for property-level encryption.
        /// Encryption profiles define how specific properties should be encrypted when stored in the database.
        /// This method is part of the Encryption Preview feature, and is subject to change or removal.
        /// </summary>
        /// <param name="propertyEncryptionProfiles">A read-only list of property encryption profiles to be used for encrypting and decrypting properties.</param>
        /// <returns>The current <see cref="ConfigBuilder"/> instance to allow method chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="propertyEncryptionProfiles"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// An element of <paramref name="propertyEncryptionProfiles"/> was not created via
        /// <see cref="PropertyEncryptionProfile"/>.
        /// </exception>
        public ConfigBuilder WithPropertyEncryptionProfiles(
            IReadOnlyList<IPropertyEncryptionProfile> propertyEncryptionProfiles)
        {
            return configBuilder.Preview_WithPropertyEncryptionProfiles(propertyEncryptionProfiles);
        }
    }
}
