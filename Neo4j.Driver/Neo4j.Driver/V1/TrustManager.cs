// Copyright (c) 2002-2018 "Neo4j,"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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

namespace Neo4j.Driver.V1
{

    /// <summary>
    /// This is the base class all built-in or custom trust manager implementations should be inheriting from. Trust managers
    /// are the way that one could customise how TLS trust is established.
    /// </summary>
    public abstract class TrustManager
    {

        internal IDriverLogger Logger { get; set; }

        /// <summary>
        /// Returns whether the endpoint should be trusted or not.
        /// </summary>
        /// <param name="uri">The uri towards which we're establishing connection</param>
        /// <param name="certificate">The certificate presented by the other endpoint</param>
        /// <param name="chain">The certificate chain that was built during the handshake</param>
        /// <param name="sslPolicyErrors">The initial policy errors that shows what problems were detected during the handshake</param>
        /// <returns><value>true</value> if the connection should be established, <value>false</value> otherwise</returns>
        public abstract bool ValidateServerCertificate(Uri uri, X509Certificate2 certificate, X509Chain chain,
            SslPolicyErrors sslPolicyErrors);

        /// <summary>
        /// Creates an insecure trust manager that trusts any certificate it is presented, but does hostname verification.
        /// </summary>
        /// <returns>An instance of <see cref="TrustManager"/></returns>
        public static TrustManager CreateInsecure() => CreateInsecure(true);

        /// <summary>
        /// Creates an insecure trust manager that trusts any certificate it is presented with configurable hostname verification.
        /// </summary>
        /// <param name="verifyHostname">Whether to perform hostname verification</param>
        /// <returns>An instance of <see cref="TrustManager"/></returns>
        public static TrustManager CreateInsecure(bool verifyHostname) => new InsecureTrustManager(verifyHostname);

        /// <summary>
        /// Creates a trust manager that establishes trust based on system certificate stores.
        /// </summary>
        /// <returns>An instance of <see cref="TrustManager"/></returns>
        public static TrustManager CreateChainTrust() => CreateChainTrust(true);

        /// <summary>
        /// Creates a trust manager that establishes trust based on system certificate stores with configurable hostname
        /// verification.
        /// </summary>
        /// <param name="verifyHostname">Whether to perform hostname verification</param>
        /// <returns>An instance of <see cref="TrustManager"/></returns>
        public static TrustManager CreateChainTrust(bool verifyHostname) => CreateChainTrust(verifyHostname,
            X509RevocationMode.NoCheck, X509RevocationFlag.ExcludeRoot, false);

        /// <summary>
        /// Creates a trust manager that establishes trust based on system certificate stores with configurable hostname
        /// verification, revocation checks.
        /// </summary>
        /// <param name="verifyHostname">Whether to perform hostname verification</param>
        /// <param name="revocationMode">The revocation check mode</param>
        /// <param name="revocationFlag">How should the revocation check should behave</param>
        /// <param name="useMachineContext">Whether to use machine context instead of user's for certificate stores</param>
        /// <returns>An instance of <see cref="TrustManager"/></returns>
        public static TrustManager CreateChainTrust(bool verifyHostname, X509RevocationMode revocationMode,
            X509RevocationFlag revocationFlag, bool useMachineContext) =>
            new ChainTrustManager(useMachineContext, verifyHostname, revocationMode, revocationFlag);

        /// <summary>
        /// Creates a trust manager that establishes trust based on TrustedPeople system certificate store.
        /// </summary>
        /// <returns>An instance of <see cref="TrustManager"/></returns>
        public static TrustManager CreatePeerTrust() => CreatePeerTrust(true);

        /// <summary>
        /// Creates a trust manager that establishes trust based on TrustedPeople system certificate store with configurable
        /// hostname verification.
        /// </summary>
        /// <param name="verifyHostname">Whether to perform hostname verification</param>
        /// <returns>An instance of <see cref="TrustManager"/></returns>
        public static TrustManager CreatePeerTrust(bool verifyHostname) => CreatePeerTrust(verifyHostname, false);

        /// <summary>
        /// Creates a trust manager that establishes trust based on TrustedPeople system certificate store with configurable
        /// hostname verification.
        /// </summary>
        /// <param name="verifyHostname">Whether to perform hostname verification</param>
        /// <param name="useMachineContext">Whether to use machine context instead of user's for certificate stores</param>
        /// <returns>An instance of <see cref="TrustManager"/></returns>
        public static TrustManager CreatePeerTrust(bool verifyHostname, bool useMachineContext) => new PeerTrustManager(useMachineContext, verifyHostname);

        /// <summary>
        /// Creates a trust manager that establishes trust based on provided list of trusted certificates.
        /// </summary>
        /// <param name="trusted">List of trusted certificates</param>
        /// <returns>An instance of <see cref="TrustManager"/></returns>
        public static TrustManager CreateCertTrust(IEnumerable<X509Certificate2> trusted) => CreateCertTrust(trusted, true);
        
        /// <summary>
        /// Creates a trust manager that establishes trust based on provided list of trusted certificates with configurable
        /// hostname verification.
        /// </summary>
        /// <param name="trusted">List of trusted certificates</param>
        /// <param name="verifyHostname">Whether to perform hostname verification</param>
        /// <returns>An instance of <see cref="TrustManager"/></returns>
        public static TrustManager CreateCertTrust(IEnumerable<X509Certificate2> trusted, bool verifyHostname) => new CertificateTrustManager(verifyHostname, trusted);

    }

}