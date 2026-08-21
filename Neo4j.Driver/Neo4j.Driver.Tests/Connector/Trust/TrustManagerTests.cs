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
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Neo4j.Driver.Internal.Logging;
using Xunit;

namespace Neo4j.Driver.Tests.Connector.Trust;

public class TrustManagerTests
{
    private static readonly Uri ServerUri = new("bolt://example.test:7687");

    public static IEnumerable<object[]> AllTrustManagers()
    {
        foreach (var verifyHostname in new[] { false, true })
        {
            yield return [TrustManager.CreateChainTrust(verifyHostname), verifyHostname];
            yield return [TrustManager.CreatePeerTrust(verifyHostname), verifyHostname];
            yield return [TrustManager.CreateCertTrust([], verifyHostname), verifyHostname];
            yield return [TrustManager.CreateInsecure(verifyHostname), verifyHostname];
        }
    }

    private static X509Certificate2 SelfSignedCertificate()
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=example.test", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
    }

    [Theory]
    [MemberData(nameof(AllTrustManagers))]
    public void ShouldRefusePeerThatPresentedNoCertificate(TrustManager trustManager, bool verifyHostname)
    {
        trustManager.Neo4JLogger = NullNeo4JLogger.Instance;

        var trusted = trustManager.ValidateServerCertificate(
            ServerUri,
            null,
            new X509Chain(),
            SslPolicyErrors.RemoteCertificateNotAvailable);

        trusted.Should().BeFalse($"verifyHostname: {verifyHostname}");
    }

    [Theory]
    [MemberData(nameof(AllTrustManagers))]
    public void ShouldRefuseCertificateAccompaniedByNotAvailableFlag(TrustManager trustManager, bool verifyHostname)
    {
        trustManager.Neo4JLogger = NullNeo4JLogger.Instance;
        using var certificate = SelfSignedCertificate();

        var trusted = trustManager.ValidateServerCertificate(
            ServerUri,
            certificate,
            new X509Chain(),
            SslPolicyErrors.RemoteCertificateNotAvailable);

        trusted.Should().BeFalse($"verifyHostname: {verifyHostname}");
    }

    [Theory]
    [MemberData(nameof(AllTrustManagers))]
    public void ShouldRefuseMissingCertificateEvenWithoutPolicyErrors(TrustManager trustManager, bool verifyHostname)
    {
        trustManager.Neo4JLogger = NullNeo4JLogger.Instance;

        var trusted = trustManager.ValidateServerCertificate(
            ServerUri,
            null,
            new X509Chain(),
            SslPolicyErrors.None);

        trusted.Should().BeFalse($"verifyHostname: {verifyHostname}");
    }

    [Fact]
    public void ShouldTrustPresentedCertificateWhenValidationIsDisabled()
    {
        var trustManager = TrustManager.CreateInsecure();
        trustManager.Neo4JLogger = NullNeo4JLogger.Instance;
        using var certificate = SelfSignedCertificate();

        var trusted = trustManager.ValidateServerCertificate(
            ServerUri,
            certificate,
            new X509Chain(),
            SslPolicyErrors.None);

        trusted.Should().BeTrue();
    }
}
