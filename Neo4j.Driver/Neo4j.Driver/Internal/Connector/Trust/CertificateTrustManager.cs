// Copyright (c) "Neo4j"
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
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Neo4j.Driver;

namespace Neo4j.Driver.Internal.Connector.Trust
{
    internal sealed class CertificateTrustManager : TrustManager
    {
        private readonly X509Certificate2Collection _trustedCertificates;
        private readonly bool _verifyHostname;

        public CertificateTrustManager(bool verifyHostname, IEnumerable<X509Certificate2> trustedCertificates)
        {
            Throw.ArgumentNullException.IfNull(trustedCertificates, nameof(trustedCertificates));

            _verifyHostname = verifyHostname;
            _trustedCertificates = new X509Certificate2Collection(trustedCertificates.ToArray());
        }

        public override bool ValidateServerCertificate(Uri uri, X509Certificate2 certificate, X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            var now = DateTime.Now;

            if (_verifyHostname)
            {
                if (sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateNameMismatch))
                {
                    Logger?.Error(null,
                        $"{GetType().Name}: Certificate '{certificate.Subject}' does not match with host name '{uri.Host}'.");
                    return false;
                }
            }

            if (!CertHelper.CheckValidity(certificate, now))
            {
                Logger?.Error(null,
                    $"{GetType().Name}: Certificate '{certificate.Subject}' is not valid at the time of validity check '{now}'.");
                return false;
            }

            for (var i = chain.ChainElements.Count - 1; i >= 0; i--)
            {
                if (CertHelper.FindCertificate(_trustedCertificates, chain.ChainElements[i].Certificate))
                {
                    Logger?.Info($"{GetType().Name}: Trusting {uri} with certificate '{certificate.Subject}'.");
                    return true;
                }
            }

            Logger?.Error(null,
                $"{GetType().Name}: Unable to locate a certificate for {uri} in provided trusted certificates.");
            return false;
        }
    }
}