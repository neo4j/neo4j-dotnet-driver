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

using FluentAssertions;
using Neo4j.Driver.TestKitBackend.Certificates;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Certificates;

// The fixture PEMs are copies of testkit's tests/tls/certs/driver files: an RSA certificate,
// its plain PKCS#1 key, and the same key as a traditional OpenSSL DEK-Info encrypted PEM
// (password "thepassword1") - the formats the backend must actually handle.
public class CertificateLoaderTests
{
    private readonly CertificateLoader _loader = new();

    private static string TestData(string file)
    {
        return Path.Combine(AppContext.BaseDirectory, "TestData", file);
    }

    [Fact]
    public void Loads_a_certificate_with_a_plain_private_key()
    {
        var certificate = _loader.Load(
            TestData("certificate1.pem"),
            TestData("privatekey1.pem"),
            null);

        certificate.HasPrivateKey.Should().BeTrue();
        certificate.Subject.Should().Contain("CN=client");
    }

    [Fact]
    public void Loads_a_certificate_with_a_password_protected_private_key()
    {
        var certificate = _loader.Load(
            TestData("certificate1.pem"),
            TestData("privatekey1_with_thepassword1.pem"),
            "thepassword1");

        certificate.HasPrivateKey.Should().BeTrue();
        certificate.Subject.Should().Contain("CN=client");
    }
}
