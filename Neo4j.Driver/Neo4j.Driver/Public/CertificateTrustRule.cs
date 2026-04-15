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

namespace Neo4j.Driver;

/// <summary>
/// Controls which server certificates the driver will accept when establishing a TLS connection.
/// Used with <see cref="ConfigBuilder.WithCertificateTrustRule(CertificateTrustRule, System.Collections.Generic.IReadOnlyList{System.Security.Cryptography.X509Certificates.X509Certificate2})"/>.
/// </summary>
public enum CertificateTrustRule
{
    /// <summary>
    /// Accept only certificates that can be verified against the operating system's trusted CA stores.
    /// This is the recommended option for production environments where the server has a certificate
    /// issued by a well-known CA.
    /// </summary>
    TrustSystem = 0,

    /// <summary>
    /// Accept only certificates that chain to one of a provided list of trusted CA certificates.
    /// Use this when connecting to a server with a certificate issued by a private or self-managed CA.
    /// The list of trusted certificates must be supplied via
    /// <see cref="ConfigBuilder.WithCertificateTrustRule(CertificateTrustRule, System.Collections.Generic.IReadOnlyList{System.Security.Cryptography.X509Certificates.X509Certificate2})"/>.
    /// </summary>
    TrustList = 1,

    /// <summary>
    /// Accept any certificate without validation.
    /// <para>
    /// <b>Warning:</b> this disables all certificate trust checks and makes the connection vulnerable
    /// to man-in-the-middle attacks. Do not use in production.
    /// </para>
    /// </summary>
    TrustAny = 2
}
