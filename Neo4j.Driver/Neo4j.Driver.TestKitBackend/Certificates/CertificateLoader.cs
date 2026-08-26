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

using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;

namespace Neo4j.Driver.TestKitBackend.Certificates;

internal interface ICertificateLoader
{
    X509Certificate2 Load(string certfile, string keyfile, string? password);
}

internal class CertificateLoader : ICertificateLoader
{
    public X509Certificate2 Load(string certfile, string keyfile, string? password)
    {
        using var certReader = new StringReader(File.ReadAllText(certfile));
        var certificate = (Org.BouncyCastle.X509.X509Certificate)new PemReader(certReader).ReadObject();

        using var keyReader = new StringReader(File.ReadAllText(keyfile));
        var key = (AsymmetricCipherKeyPair)new PemReader(keyReader, new PasswordProvider(password)).ReadObject();

        var store = new Pkcs12StoreBuilder().Build();
        store.SetKeyEntry("key", new AsymmetricKeyEntry(key.Private), [new X509CertificateEntry(certificate)]);

        using var pkcsStream = new MemoryStream();
        store.Save(pkcsStream, password?.ToCharArray(), new SecureRandom());
        return X509CertificateLoader.LoadPkcs12(pkcsStream.ToArray(), password, X509KeyStorageFlags.Exportable);
    }

    private class PasswordProvider : IPasswordFinder
    {
        private readonly string? _password;

        public PasswordProvider(string? password)
        {
            _password = password;
        }

        public char[] GetPassword()
        {
            return _password?.ToCharArray() ?? [];
        }
    }
}
