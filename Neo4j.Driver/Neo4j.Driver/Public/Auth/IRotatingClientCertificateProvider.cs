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

namespace Neo4j.Driver;

/// <summary>
/// This interface allows for the implementation of a client certificate provider that can be updated with
/// new certificates.
/// </summary>
public interface IRotatingClientCertificateProvider : IClientCertificateProvider
{
    /// <summary>
    /// Updates the certificate stored in the provider.
    /// <para/>
    /// To be called by user-code when a new client certificate is available. This method must be thread-safe.
    /// </summary>
    /// <param name="certificate">The new certificate.</param>
    void UpdateCertificate(X509Certificate certificate);
}
