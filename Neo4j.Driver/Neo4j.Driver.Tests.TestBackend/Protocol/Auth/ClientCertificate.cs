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
using System.Security.Cryptography.X509Certificates;

namespace Neo4j.Driver.Tests.TestBackend.Protocol.Auth;

internal class ClientCertificate : ProtocolObject
{
    public ClientCertificateType data { get; set; } = new();

    private Lazy<X509Certificate2> _certificate;
    public X509Certificate2 Certificate => _certificate.Value;

    /// <inheritdoc />
    public ClientCertificate()
    {
        _certificate = new Lazy<X509Certificate2>(
            () => ClientCertificateLoader.GetCertificate(data.certfile, data.keyfile, data.password));
    }

    public class ClientCertificateType
    {
        public string certfile { get; set; } = "";
        public string keyfile { get; set; } = "";
        public string password { get; set; } = "";
    }
}

internal static class ClientCertificateLoader
{
    public static X509Certificate2 GetCertificate(string certfile, string keyfile, string password)
    {
        return string.IsNullOrEmpty(password)
            ? X509Certificate2.CreateFromPemFile(certfile, keyfile)
            : X509Certificate2.CreateFromEncryptedPemFile(certfile, password, keyfile);
    }
}
