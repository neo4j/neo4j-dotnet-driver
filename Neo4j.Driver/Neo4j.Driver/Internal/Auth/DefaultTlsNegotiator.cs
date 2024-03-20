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
using System.IO;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Neo4j.Driver.Internal.Auth;

internal sealed class DefaultTlsNegotiator : ITlsNegotiator
{
    private readonly ILogger _logger;
    private readonly EncryptionManager _encryptionManager;

    public DefaultTlsNegotiator(ILogger logger, EncryptionManager encryptionManager)
    {
        _logger = logger;
        _encryptionManager = encryptionManager;
    }

    /// <inheritdoc />
    public SslStream NegotiateTls(Uri uri, Stream stream)
    {
        return new SslStream(
            stream,
            true,
            (_, certificate, chain, errors) =>
            {
                if (errors.HasFlag(SslPolicyErrors.RemoteCertificateNotAvailable))
                {
                    _logger?.Error(null, $"{GetType().Name}: Certificate not available.");
                    return false;
                }

                var trust = _encryptionManager.TrustManager.ValidateServerCertificate(
                    uri,
                    new X509Certificate2(certificate.Export(X509ContentType.Cert)),
                    chain,
                    errors);

                if (trust)
                {
                    _logger?.Debug("Trust is established, resuming connection.");
                }
                else
                {
                    _logger?.Error(null, "Trust not established, aborting communication.");
                }

                return trust;
            });
    }
}
