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
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Neo4j.Driver.Internal.Connector.Trust;

namespace Neo4j.Driver;

/// <summary>
/// Base class for TLS trust managers. A trust manager decides whether to accept a server's TLS certificate
/// during connection establishment.
/// </summary>
/// <remarks>
/// <para>
/// For most use cases, certificate trust is configured through the URI scheme rather than a
/// <see cref="TrustManager"/>. Use <c>bolt+s://</c> or <c>neo4j+s://</c> for standard CA-based
/// trust, or <c>bolt+ssc://</c> / <c>neo4j+ssc://</c> to accept self-signed server certificates.
/// </para>
/// <para>
/// Use a <see cref="TrustManager"/> (via <see cref="ConfigBuilder.WithTrustManager"/>) when you
/// need fine-grained control over certificate validation beyond what the URI scheme provides —
/// for example, to pin specific CA certificates or customise revocation checking.
/// </para>
/// </remarks>
public abstract class TrustManager
{
    internal INeo4jLogger Neo4JLogger { get; set; }

    /// <summary>Returns whether the endpoint should be trusted or not.</summary>
    /// <param name="uri">The uri towards which we're establishing connection</param>
    /// <param name="certificate">The certificate presented by the other endpoint</param>
    /// <param name="chain">The certificate chain that was built during the handshake</param>
    /// <param name="sslPolicyErrors">The initial policy errors that shows what problems were detected during the handshake</param>
    /// <returns>
    /// <value>true</value>
    /// if the connection should be established,
    /// <value>false</value>
    /// otherwise
    /// </returns>
    public abstract bool ValidateServerCertificate(
        Uri uri,
        X509Certificate2 certificate,
        X509Chain chain,
        SslPolicyErrors sslPolicyErrors);

    /// <summary>
    /// Creates a trust manager that accepts any certificate without validation.
    /// </summary>
    /// <param name="verifyHostname">Whether to verify that the server hostname matches the certificate's subject.</param>
    /// <returns>An instance of <see cref="TrustManager"/>.</returns>
    /// <remarks>
    /// <b>Warning:</b> this trust manager disables certificate validation and makes connections vulnerable to
    /// man-in-the-middle attacks. It should not be used in production. Consider using
    /// <see cref="CreateChainTrust"/> or <see cref="CreateCertTrust"/> instead.
    /// </remarks>
    public static TrustManager CreateInsecure(bool verifyHostname = false)
    {
        return new InsecureTrustManager(verifyHostname);
    }

    /// <summary>
    /// Creates a trust manager that validates server certificates against the operating system's trusted CA stores.
    /// This is the recommended option for production environments where the server has a certificate issued by a
    /// well-known CA.
    /// </summary>
    /// <param name="verifyHostname">Whether to verify that the server hostname matches the certificate's subject. Defaults to <c>true</c>.</param>
    /// <param name="revocationMode">Controls how certificate revocation is checked. Defaults to <see cref="X509RevocationMode.NoCheck"/>.</param>
    /// <param name="revocationFlag">Controls which certificates in the chain are checked for revocation. Defaults to <see cref="X509RevocationFlag.ExcludeRoot"/>.</param>
    /// <param name="useMachineContext">Whether to use the machine-level certificate store rather than the current user's store. Defaults to <c>false</c>.</param>
    /// <returns>An instance of <see cref="TrustManager"/>.</returns>
    public static TrustManager CreateChainTrust(
        bool verifyHostname = true,
        X509RevocationMode revocationMode = X509RevocationMode.NoCheck,
        X509RevocationFlag revocationFlag = X509RevocationFlag.ExcludeRoot,
        bool useMachineContext = false)
    {
        return new ChainTrustManager(useMachineContext, verifyHostname, revocationMode, revocationFlag);
    }

    /// <summary>
    /// Creates a trust manager that validates server certificates against the
    /// <see href="https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.storename">TrustedPeople</see>
    /// system certificate store.
    /// </summary>
    /// <param name="verifyHostname">Whether to verify that the server hostname matches the certificate's subject. Defaults to <c>true</c>.</param>
    /// <param name="useMachineContext">Whether to use the machine-level certificate store rather than the current user's store. Defaults to <c>false</c>.</param>
    /// <returns>An instance of <see cref="TrustManager"/>.</returns>
    public static TrustManager CreatePeerTrust(bool verifyHostname = true, bool useMachineContext = false)
    {
        return new PeerTrustManager(useMachineContext, verifyHostname);
    }

    /// <summary>
    /// Creates a trust manager that validates server certificates against a provided list of trusted CA certificates.
    /// Use this when connecting to a server whose certificate was issued by a private or self-managed CA.
    /// </summary>
    /// <param name="trusted">The list of trusted CA certificates to validate against.</param>
    /// <param name="verifyHostname">Whether to verify that the server hostname matches the certificate's subject. Defaults to <c>true</c>.</param>
    /// <returns>An instance of <see cref="TrustManager"/>.</returns>
    public static TrustManager CreateCertTrust(IEnumerable<X509Certificate2> trusted, bool verifyHostname = true)
    {
        return new CertificateTrustManager(verifyHostname, trusted);
    }
}
