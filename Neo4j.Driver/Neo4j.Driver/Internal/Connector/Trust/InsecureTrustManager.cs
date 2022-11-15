// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License"):
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
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Neo4j.Driver.Internal.Connector.Trust;

internal sealed class InsecureTrustManager : TrustManager
{
    public InsecureTrustManager(bool verifyHostname)
    {
        VerifyHostName = verifyHostname;
    }

    public bool VerifyHostName { get; }

    public override bool ValidateServerCertificate(
        Uri uri,
        X509Certificate2 certificate,
        X509Chain chain,
        SslPolicyErrors sslPolicyErrors)
    {
        if (VerifyHostName)
        {
            if (sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateNameMismatch))
            {
                Logger?.Error(
                    null,
                    $"{GetType().Name}: Certificate '{certificate.Subject}' does not match with host name '{uri.Host}'.");

                return false;
            }
        }

        Logger?.Info($"{GetType().Name}: Trusting {uri} with provided certificate '{certificate.Subject}'.");
        return true;
    }
}
