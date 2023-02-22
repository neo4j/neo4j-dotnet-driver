// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using X509Certificate = Org.BouncyCastle.X509.X509Certificate;

namespace Neo4j.Driver.IntegrationTests.Internals;

public static class CertificateUtils
{
    public static Pkcs12Store CreateCert(
        string commonName,
        DateTime notBefore,
        DateTime notAfter,
        IEnumerable<string> dnsAltNames,
        IEnumerable<string> ipAddressAltNames,
        Pkcs12Store signBy)
    {
        var keyPairGen = new RsaKeyPairGenerator();
        keyPairGen.Init(new KeyGenerationParameters(new SecureRandom(), 2048));

        var keyPair = keyPairGen.GenerateKeyPair();

        var certGenerator = new X509V3CertificateGenerator();
        certGenerator.SetSubjectDN(new X509Name($"CN={commonName}"));
        if (signBy == null)
        {
            certGenerator.SetIssuerDN(new X509Name($"CN={commonName}"));
        }
        else
        {
            certGenerator.SetIssuerDN(signBy.GetCertificate().SubjectDN);
        }

        certGenerator.SetSerialNumber(BigInteger.ProbablePrime(64, new Random()));
        certGenerator.SetNotBefore(notBefore);
        certGenerator.SetNotAfter(notAfter);
        certGenerator.SetPublicKey(keyPair.Public);

        var altNames = dnsAltNames?.ToArray() ?? Array.Empty<string>();
        var addressAltNames = ipAddressAltNames?.ToArray() ?? Array.Empty<string>();
        if (altNames.Any() || addressAltNames.Any())
        {
            var alternativeNames = new List<Asn1Encodable>();
            
            alternativeNames.AddRange(
                altNames.Select(name => new GeneralName(GeneralName.DnsName, name)));

            alternativeNames.AddRange(
                addressAltNames.Select(ip => new GeneralName(GeneralName.IPAddress, ip)));

            certGenerator.AddExtension(
                X509Extensions.SubjectAlternativeName,
                false,
                new DerSequence(alternativeNames.ToArray()));
        }

        var signatureKeyPair = signBy != null
            ? new AsymmetricCipherKeyPair(signBy.GetCertificate().GetPublicKey(), signBy.GetKey())
            : keyPair;

        var signer = new Asn1SignatureFactory("SHA256WITHRSA", signatureKeyPair.Private);
        var certificate = certGenerator.Generate(signer);

        return ToPkcs12(certificate, keyPair.Private);
    }

    private static Pkcs12Store ToPkcs12(X509Certificate certificate, AsymmetricKeyParameter privateKey)
    {
        var pkcs12Store = new Pkcs12Store();
        var certificateEntry = new X509CertificateEntry(certificate);
        var certificateAlias = certificate.SubjectDN.ToString();

        pkcs12Store.SetCertificateEntry(certificateAlias, certificateEntry);
        pkcs12Store.SetKeyEntry(
            certificate.SubjectDN.ToString(),
            new AsymmetricKeyEntry(privateKey),
            new[] { certificateEntry });

        return pkcs12Store;
    }

    public static X509Certificate GetCertificate(this Pkcs12Store store)
    {
        foreach (string alias in store.Aliases)
        {
            var keyEntry = store.GetKey(alias);
            if (keyEntry.Key.IsPrivate)
            {
                return store.GetCertificate(alias).Certificate;
            }
        }

        throw new ArgumentException("Invalid store.");
    }

    public static X509Certificate2 GetDotnetCertificate(this Pkcs12Store store)
    {
        var stream = new MemoryStream();
        var password = "password";

        store.Save(stream, password.ToCharArray(), SecureRandom.GetInstance("SHA256PRNG"));

        var dotnetCertificate = new X509Certificate2(
            stream.ToArray(),
            password,
            X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);

        return dotnetCertificate;
    }

    public static AsymmetricKeyParameter GetKey(this Pkcs12Store store)
    {
        foreach (string alias in store.Aliases)
        {
            var keyEntry = store.GetKey(alias);
            if (keyEntry.Key.IsPrivate)
            {
                return keyEntry.Key;
            }
        }

        throw new ArgumentException("Invalid store.");
    }

    public static void DumpPem(AsymmetricKeyParameter value, string target)
    {
        using (var targetStream = new FileStream(target, FileMode.OpenOrCreate))
        {
            using (var targetWriter = new StreamWriter(targetStream, Encoding.ASCII))
            {
                var pemWriter = new PemWriter(targetWriter);
                pemWriter.WriteObject(new Pkcs8Generator(value));
            }
        }
    }

    public static void DumpPem(X509Certificate value, string target)
    {
        using (var targetStream = new FileStream(target, FileMode.OpenOrCreate))
        {
            using (var targetWriter = new StreamWriter(targetStream, Encoding.ASCII))
            {
                var pemWriter = new PemWriter(targetWriter);
                pemWriter.WriteObject(new MiscPemGenerator(value));
            }
        }
    }
}
