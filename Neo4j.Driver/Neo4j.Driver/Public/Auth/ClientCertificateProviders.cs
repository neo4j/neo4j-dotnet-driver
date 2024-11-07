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

using System.Security.Cryptography.X509Certificates;
using Neo4j.Driver.Internal.Auth;

namespace Neo4j.Driver;

/// <summary>
/// This class provides static methods to create basic client certificate providers.
/// </summary>
public static class ClientCertificateProviders
{
    /// <summary>
    /// Creates a new static client certificate provider with the given certificate.
    /// </summary>
    /// <param name="certificate">The certificate.</param>
    /// <returns>The new static client certificate provider.</returns>
    public static IClientCertificateProvider Static(X509Certificate certificate)
    {
        return new StaticClientCertificateProvider(certificate);
    }

    /// <summary>
    /// Creates a new rotating client certificate provider with the given certificate as the initial certificate.
    /// </summary>
    /// <param name="certificate">The initial certificate.</param>
    /// <returns>The new rotating client certificate provider.</returns>
    public static IRotatingClientCertificateProvider Rotating(X509Certificate certificate)
    {
        return new RotatingClientCertificateProvider(certificate);
    }
}
