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
using Microsoft.Extensions.Logging;
using Moq;
using Neo4j.Driver.TestKitBackend.Certificates;
using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Continuations;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.ObjectRegistry;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

// The provider handler and the completed handler only make sense as a pair — this pins the
// callback handshake between them via a real IContinuationCoordinator, playing the roles of the
// driver (asking for the connection certificate) and of the detached operation whose response
// slot the callback borrows.
public class ClientCertificateProviderFlowTests
{
    private record TerminalResponse(string Tag) : IProtocolMessage;

    private readonly ContinuationCoordinator _coordinator = new();
    private readonly Mock<IResponseWriter> _responseWriterMock = new();
    private readonly Mock<ICertificateLoader> _certificateLoaderMock = new();

    private IClientCertificateProvider RegisterProvider()
    {
        IClientCertificateProvider? provider = null;
        var registryMock = new Mock<IRegistry>();
        registryMock
            .Setup(r => r.Register(It.IsAny<IClientCertificateProvider>()))
            .Returns<IClientCertificateProvider>(
                p =>
                {
                    provider = p;
                    return new RegistryObject<IClientCertificateProvider>("provider-1", p);
                });

        var newProviderHandler = new NewClientCertificateProviderHandler(
            registryMock.Object,
            _coordinator,
            _certificateLoaderMock.Object,
            _responseWriterMock.Object,
            Mock.Of<ILogger>());

        newProviderHandler.ProcessAsync(new NewClientCertificateProviderRequest()).GetAwaiter().GetResult();

        Assert.NotNull(provider);
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

    private async Task<X509Certificate> RoundTripCertificateAsync(
        IClientCertificateProvider provider,
        ClientCertificate wireCertificate)
    {
        var openRequestTask = _coordinator.WaitForNextResponseAsync();
        var certificateTask = provider.GetCertificateAsync();

        var callbackRequest = Assert.IsType<ClientCertificateProviderRequest>(
            await WithTimeoutAsync(openRequestTask));

        Assert.Equal("provider-1", callbackRequest.ClientCertificateProviderId);

        var completedHandler = new CallbackCompletedHandler<ClientCertificateProviderCompletedRequest>(
            _coordinator,
            _responseWriterMock.Object);

        var completedTask = completedHandler.ProcessAsync(
            new ClientCertificateProviderCompletedRequest
            {
                RequestId = callbackRequest.Id,
                HasUpdate = false,
                ClientCertificate = wireCertificate
            });

        var certificate = await WithTimeoutAsync(certificateTask.AsTask());

        // The resumed operation eventually produces the terminal response; the completed handler
        // is the one holding the response slot, so it writes it.
        _coordinator.CompleteNextResponse(new TerminalResponse("result"));
        await WithTimeoutAsync(completedTask);

        return certificate;
    }

    [Fact]
    public async Task The_registered_provider_round_trips_a_callback_for_its_certificate()
    {
        using var certificate = CreateCertificate();
        _certificateLoaderMock
            .Setup(l => l.Load("cert.pem", "key.pem", "secret"))
            .Returns(certificate);

        var provider = RegisterProvider();

        _responseWriterMock.Verify(
            w => w.WriteAsync(new ClientCertificateProviderResponse("provider-1")),
            Times.Once);

        var roundTripped = await RoundTripCertificateAsync(
            provider,
            new ClientCertificate("cert.pem", "key.pem", "secret"));

        Assert.Same(certificate, roundTripped);
        _responseWriterMock.Verify(w => w.WriteAsync(new TerminalResponse("result")), Times.Once);
    }

    [Fact]
    public async Task Every_ask_is_relayed_so_a_rotated_certificate_is_picked_up()
    {
        using var firstCertificate = CreateCertificate();
        using var secondCertificate = CreateCertificate();
        _certificateLoaderMock
            .Setup(l => l.Load("cert1.pem", "key1.pem", null))
            .Returns(firstCertificate);
        _certificateLoaderMock
            .Setup(l => l.Load("cert2.pem", "key2.pem", null))
            .Returns(secondCertificate);

        var provider = RegisterProvider();

        var first = await RoundTripCertificateAsync(provider, new ClientCertificate("cert1.pem", "key1.pem"));
        var second = await RoundTripCertificateAsync(provider, new ClientCertificate("cert2.pem", "key2.pem"));

        Assert.Same(firstCertificate, first);
        Assert.Same(secondCertificate, second);
    }

    private static async Task<T> WithTimeoutAsync<T>(Task<T> task)
    {
        var completed = await Task.WhenAny(
            task,
            Task.Delay(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));

        Assert.Same(task, completed);
        return await task;
    }

    private static async Task WithTimeoutAsync(Task task)
    {
        var completed = await Task.WhenAny(
            task,
            Task.Delay(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));

        Assert.Same(task, completed);
        await task;
    }
}
