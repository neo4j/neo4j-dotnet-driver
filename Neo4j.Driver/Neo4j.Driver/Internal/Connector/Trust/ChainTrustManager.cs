// Copyright (c) 2002-2020 "Neo4j,"
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
    internal class ChainTrustManager: TrustManager
    {
        private static readonly TimeSpan OnlineTimeout = new TimeSpan(0, 0, 1, 0);

        private readonly bool _useMachineCtx;
        private readonly bool _verifyHostname;
        private readonly X509RevocationMode _revocationMode;
        private readonly X509RevocationFlag _revocationScope;

        public ChainTrustManager(bool useMachineCtx, bool verifyHostname, X509RevocationMode revocationMode,
            X509RevocationFlag revocationScope)
        {
            _useMachineCtx = useMachineCtx;
            _verifyHostname = verifyHostname;
            _revocationMode = revocationMode;
            _revocationScope = revocationScope;
        }

        public override bool ValidateServerCertificate(Uri uri, X509Certificate2 certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (_verifyHostname)
            {
                if (sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateNameMismatch))
                {
                    Logger?.Error(null, $"{GetType().Name}: Certificate '{certificate.Subject}' does not match with host name '{uri.Host}'.");
                    return false;
                }
            }

            var result = !sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateChainErrors);
            if (_useMachineCtx || chain.ChainPolicy.RevocationFlag != _revocationScope ||
                chain.ChainPolicy.RevocationMode != _revocationMode)
            {
                result = BuildChain(certificate, chain.ChainPolicy.ExtraStore, out var newChain);
                if (!result)
                {
                    Logger?.Error(null,
                        $"{GetType().Name}: Certificate '{certificate.Subject}' failed validation. Reason: {CertHelper.ChainStatusToText(newChain.ChainStatus)}");
                }
            }

            return result;
        }

        protected bool BuildChain(X509Certificate2 certificate, X509Certificate2Collection additionalCerts, out X509Chain chain)
        {
            var time = DateTime.Now;
#if NET452
            var newChain = new X509Chain(_useMachineCtx)
#else
            var newChain = new X509Chain()
#endif
            {
                ChainPolicy =
                {
                    RevocationMode = _revocationMode,
                    RevocationFlag = _revocationScope,
                    UrlRetrievalTimeout = OnlineTimeout,
                    VerificationTime = time
                }
            };

            newChain.ChainPolicy.ExtraStore.AddRange(additionalCerts);

            var result = newChain.Build(certificate);
            chain = newChain;
            return result;
        }

    }
}