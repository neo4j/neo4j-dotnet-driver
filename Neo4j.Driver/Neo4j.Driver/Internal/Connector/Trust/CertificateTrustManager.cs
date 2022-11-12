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

namespace Neo4j.Driver.Internal.Connector.Trust
{
    internal sealed class CertificateTrustManager : TrustManager
    {
        private readonly X509Certificate2Collection _trustedCertificates;
        private readonly bool _verifyHostname;

        public CertificateTrustManager(bool verifyHostname, IEnumerable<X509Certificate2> trustedCertificates)
        {
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
                        $"{nameof(CertificateTrustManager)}: Certificate '{certificate.Subject}' does not match with host name '{uri.Host}'.");
                    return false;
                }
            }

            if (!CertHelper.CheckValidity(certificate, now))
            {
                Logger?.Error(null,
                    $"{nameof(CertificateTrustManager)}: Certificate '{certificate.Subject}' is not valid at the time of validity check '{now}'.");
                return false;
            }

            return ValidateChain(uri, certificate, IsValidChain(chain) ? chain : CreateChainAgainstStore(certificate));
        }

        private static bool IsValidChain(X509Chain chain) =>
            chain.ChainStatus.All(x => x.Status == X509ChainStatusFlags.NoError);

        private X509Chain CreateChainAgainstStore(X509Certificate2 certificate)
        {
            Logger?.Info($"{nameof(CertificateTrustManager)}: Building chain against extra store certificate '{certificate.Subject}'.");

            // build chain against certificates passed, as some may not have been used when .net assessed trust.
            var extraStoreChain = new X509Chain();
            extraStoreChain.ChainPolicy.ExtraStore.AddRange(_trustedCertificates);
            extraStoreChain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
            extraStoreChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            extraStoreChain.Build(certificate);
            return extraStoreChain;
        }

        private bool ValidateChain(Uri uri, X509Certificate2 certificate, X509Chain chain)
        {
            if (chain.ChainStatus.Any(ShouldFailChain))
            {
                Logger?.Error(null,
                    $"{nameof(CertificateTrustManager)}: Unable to locate a certificate for {uri} in provided trusted certificates.");
                return false;
            }

            for (var i = chain.ChainElements.Count - 1; i >= 0; i--)
            {
                if (CertHelper.FindCertificate(_trustedCertificates, chain.ChainElements[i].Certificate))
                {
                    Logger?.Info($"{nameof(CertificateTrustManager)}: Trusting {uri} with certificate '{certificate.Subject}'.");
                    return true;
                }
            }

            Logger?.Error(null,
                $"{nameof(CertificateTrustManager)}: Unable to locate a certificate for {uri} in provided trusted certificates.");

            return false;
        }

        private bool ShouldFailChain(X509ChainStatus arg)
        {
            return arg.Status switch
            {
                X509ChainStatusFlags.NoError => false,
                X509ChainStatusFlags.UntrustedRoot => false,
                _ => true
            };
        }
    }
}