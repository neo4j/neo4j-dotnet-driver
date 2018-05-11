// Copyright (c) 2002-2018 Neo4j Sweden AB [http://neo4j.com]
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
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Connector
{
    internal interface ITrustStrategy
    {
        bool ValidateServerCertificate(Uri uri, X509Certificate certificate, SslPolicyErrors sslPolicyErrors);
    }

    internal class TrustSystemCaSignedCertificates : ITrustStrategy
    {
        private readonly ILogger _logger;
        public TrustSystemCaSignedCertificates(ILogger logger)
        {
            _logger = logger;
        }
        public bool ValidateServerCertificate(Uri uri, X509Certificate certificate, SslPolicyErrors sslPolicyErrors)
        {
            var trust = true;

            if (sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateNotAvailable))
            {
                _logger?.Error("Certificate not available.");
                trust = false;
            }

            if (sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateChainErrors))
            {
                _logger?.Error("Certificate validation failed.");
                trust = false;
            }

            if (sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateNameMismatch))
            {
                _logger?.Error("Server name mismatch.");
                trust = false;
            }
            
            return trust;
        }
    }

    internal class TrustAllCertificates : ITrustStrategy
    {
        private readonly ILogger _logger;

        public TrustAllCertificates(ILogger logger)
        {
            _logger = logger; 
        }
            
        public bool ValidateServerCertificate(Uri uri, X509Certificate certificate, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateNotAvailable))
            {
                _logger?.Error("Certificate not available.");
                return false;
            }

            return true;
        }
    }
}
