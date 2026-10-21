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
/// <see cref="IDriver"/> and <see cref="ConfigBuilder"/>. This class is part
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
        /// <exception cref="ArgumentException">The driver was not created by <see cref="GraphDatabase"/>.</exception>
        public IPropertyEncryption PropertyEncryption()
        {
            if (driver is not IInternalDriver internalDriver)
            {
                throw new ArgumentException(
                    "Property encryption is only available on a driver created by GraphDatabase.Driver.",
                    nameof(driver));
            }

            return internalDriver.PropertyEncryption();
        }
    }

    extension(ConfigBuilder configBuilder)
    {
        /// <summary>
        /// Configures the property encryption profiles available through
        /// <c>driver.PropertyEncryption()</c>. This method is part of the Encryption Preview feature, and
        /// is subject to change or removal.
        /// </summary>
        /// <param name="propertyEncryptionProfiles">The profiles, each with a unique name.</param>
        /// <returns>The current <see cref="ConfigBuilder"/> instance to allow method chaining.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="propertyEncryptionProfiles"/> is <see langword="null"/> or contains a <see langword="null"/> element.
        /// </exception>
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
