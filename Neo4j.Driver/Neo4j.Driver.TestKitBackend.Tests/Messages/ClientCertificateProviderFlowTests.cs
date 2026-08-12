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

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Neo4j.Driver.TestKitBackend.Certificates;
using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Expectations;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.ObjectStorage;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class ClientCertificateProviderFlowTests
{
    private readonly Mock<IOutboundRoundTrip> _roundTripMock = new();
    private readonly Mock<IResponseWriter> _responseWriterMock = new();

    private IClientCertificateProvider StoreProvider()
    {
        IClientCertificateProvider? provider = null;
        var objectStoreMock = new Mock<IObjectStore>();
        objectStoreMock
            .Setup(r => r.Store(It.IsAny<Func<string, IClientCertificateProvider>>()))
            .Returns<Func<string, IClientCertificateProvider>>(
                create =>
                {
                    provider = create("provider-1");
                    return new Stored<IClientCertificateProvider>("provider-1", provider);
                });

        var newProviderHandler = new NewClientCertificateProviderHandler(
            objectStoreMock.Object,
            _roundTripMock.Object,
            _responseWriterMock.Object,
            Mock.Of<ILogger>());

        newProviderHandler.ProcessAsync(new NewClientCertificateProviderRequest()).GetAwaiter().GetResult();

        provider.Should().NotBeNull();
        return provider!;
    }

    private static X509Certificate2 CreateCertificate()
    {
        using var key = RSA.Create();
        var request = new CertificateRequest(
            "CN=provider-flow-test",
            key,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        return request.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1));
    }

    private async Task<(X509Certificate Certificate, ClientCertificateProviderRequest Request)> RoundTripCertificateAsync(
        IClientCertificateProvider provider,
        X509Certificate certificateToReturn)
    {
        ICorrelatedRequest? capturedRequest = null;
        _roundTripMock
            .Setup(r => r.SendExpectingAsync<X509Certificate>(It.IsAny<ICorrelatedRequest>()))
            .Callback<ICorrelatedRequest>(request => capturedRequest = request)
            .ReturnsAsync(certificateToReturn);

        var certificate = await provider.GetCertificateAsync();

        var request = capturedRequest.Should().BeOfType<ClientCertificateProviderRequest>().Subject;
        return (certificate, request);
    }

    [Fact]
    public async Task The_stored_provider_requests_a_callback_for_its_certificate()
    {
        using var certificate = CreateCertificate();
        var provider = StoreProvider();

        _responseWriterMock.Verify(
            w => w.WriteAsync(new ClientCertificateProviderResponse("provider-1")),
            Times.Once);

        var (roundTripped, request) = await RoundTripCertificateAsync(provider, certificate);

        request.ClientCertificateProviderId.Should().Be("provider-1");
        roundTripped.Should().BeSameAs(certificate);
    }

    [Fact]
    public async Task Every_ask_is_relayed_so_a_rotated_certificate_is_picked_up()
    {
        using var firstCertificate = CreateCertificate();
        using var secondCertificate = CreateCertificate();
        var provider = StoreProvider();

        var (first, _) = await RoundTripCertificateAsync(provider, firstCertificate);
        var (second, _) = await RoundTripCertificateAsync(provider, secondCertificate);

        first.Should().BeSameAs(firstCertificate);
        second.Should().BeSameAs(secondCertificate);
    }

    [Fact]
    public void ClientCertificateProviderCompleted_fulfils_the_expectation_with_the_loaded_certificate()
    {
        var expectationsMock = new Mock<IExpectationStore>();
        var certificateLoaderMock = new Mock<ICertificateLoader>();
        using var certificate = CreateCertificate();
        certificateLoaderMock.Setup(l => l.Load("cert.pem", "key.pem", null)).Returns(certificate);

        var handler = new ClientCertificateProviderCompletedHandler(expectationsMock.Object, certificateLoaderMock.Object);
        var message = new ClientCertificateProviderCompleted
        {
            RequestId = "callback-1",
            HasUpdate = true,
            ClientCertificate = new ClientCertificate("cert.pem", "key.pem")
        };

        handler.ProcessAsync(message);

        expectationsMock.Verify(e => e.Fulfil<X509Certificate>("callback-1", certificate), Times.Once);
    }
}
