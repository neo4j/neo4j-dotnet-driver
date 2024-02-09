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
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Neo4j.Driver.Internal.Helpers;

namespace Neo4j.Driver.Internal.Connector.Trust;

internal sealed class PeerTrustManager : TrustManager
{
    private readonly StoreLocation _location;
    private readonly bool _verifyHostname;

    public PeerTrustManager(bool useMachineCtx, bool verifyHostname)
    {
        _location = useMachineCtx ? StoreLocation.LocalMachine : StoreLocation.CurrentUser;
        _verifyHostname = verifyHostname;
    }

    public override bool ValidateServerCertificate(
        Uri uri,
        X509Certificate2 certificate,
        X509Chain chain,
        SslPolicyErrors sslPolicyErrors)
    {
        var now = DateTime.Now;

        if (_verifyHostname)
        {
            if (sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateNameMismatch))
            {
                Logger.Error(
                    null,
                    $"{nameof(PeerTrustManager)}: Certificate '{certificate.Subject}' does not match with host name '{uri.Host}'.");

                return false;
            }
        }

        if (!CertHelper.CheckValidity(certificate, now))
        {
            Logger.Error(
                null,
                $"{nameof(PeerTrustManager)}: Certificate '{certificate.Subject}' is not valid at the time of validity check '{now}'.");

            return false;
        }

        if (CertHelper.FindCertificate(_location, StoreName.TrustedPeople, certificate))
        {
            if (CertHelper.FindCertificate(_location, StoreName.Disallowed, certificate))
            {
                Logger.Error(
                    null,
                    $"{nameof(PeerTrustManager)}: Certificate '{certificate.Subject}' is found in '{_location}\\Disallowed` store.");

                return false;
            }

            Logger.Info($"{nameof(PeerTrustManager)}: Trusting {uri} with certificate '{certificate.Subject}'.");
            return true;
        }

        Logger.Error(
            null,
            $"{nameof(PeerTrustManager)}: Unable to locate a certificate for {uri} in '{_location}\\TrustedPeople` store.");

        return false;
    }
}
