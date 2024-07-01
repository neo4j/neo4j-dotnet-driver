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
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Pkcs;
using X509Certificate = Org.BouncyCastle.X509.X509Certificate;

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
        // Read the certificate
        var certText = File.ReadAllText(certfile);
        var certReader = new PemReader(new StringReader(certText));
        var cert = (X509Certificate)certReader.ReadObject();

        // Read the key
        var keyText = File.ReadAllText(keyfile);
        var keyReader = new PemReader(new StringReader(keyText), new PasswordProvider(password));
        var key = (AsymmetricCipherKeyPair)keyReader.ReadObject();

        // Create PKCS12 store
        var store = new Pkcs12StoreBuilder().Build();
        store.SetKeyEntry("key", new AsymmetricKeyEntry(key.Private), new[] { new X509CertificateEntry(cert) });

        // Export to .NET X509Certificate2
        using var pkcsStream = new MemoryStream();
        store.Save(pkcsStream, password?.ToCharArray(), new SecureRandom());
        return new X509Certificate2(pkcsStream.ToArray(), password, X509KeyStorageFlags.Exportable);
    }

    private class PasswordProvider : IPasswordFinder
    {
        private readonly string _password;

        public PasswordProvider(string password)
        {
            _password = password;
        }

        public char[] GetPassword() => _password.ToCharArray();
    }
}
