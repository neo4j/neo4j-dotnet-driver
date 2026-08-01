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
using Microsoft.Extensions.Logging;
using Neo4j.Driver.TestKitBackend.Certificates;
using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Continuations;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.ObjectRegistry;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal record NewClientCertificateProviderRequest : IProtocolMessage;

internal record ClientCertificateProviderResponse(string Id) : IProtocolMessage;

internal class NewClientCertificateProviderHandler : MessageHandler<NewClientCertificateProviderRequest>
{
    private readonly IRegistry _registry;
    private readonly IContinuationCoordinator _coordinator;
    private readonly ICertificateLoader _certificateLoader;
    private readonly IResponseWriter _responseWriter;
    private readonly ILogger _logger;

    public NewClientCertificateProviderHandler(
        IRegistry registry,
        IContinuationCoordinator coordinator,
        ICertificateLoader certificateLoader,
        IResponseWriter responseWriter,
        ILogger logger)
    {
        _registry = registry;
        _coordinator = coordinator;
        _certificateLoader = certificateLoader;
        _responseWriter = responseWriter;
        _logger = logger;
    }

    public override async Task ProcessAsync(NewClientCertificateProviderRequest message)
    {
        var providerId = "";
        IClientCertificateProvider provider =
            new TestKitClientCertificateProvider(() => ProvideCertificateAsync(providerId));

        var registered = _registry.Register(provider);
        providerId = registered.Id;

        _logger.LogDebug("Created client certificate provider with id '{Id}'", registered.Id);
        await _responseWriter.WriteAsync(new ClientCertificateProviderResponse(registered.Id));
    }

    private async ValueTask<X509Certificate> ProvideCertificateAsync(string providerId)
    {
        var pending = _coordinator.RegisterCallback();
        _coordinator.CompleteNextResponse(new ClientCertificateProviderRequest(pending.Id, providerId));

        var completion = (ClientCertificateProviderCompletedRequest)await pending.Completion;
        var certificate = completion.ClientCertificate.Value;
        return _certificateLoader.Load(certificate.Certfile, certificate.Keyfile, certificate.Password);
    }
}

internal class TestKitClientCertificateProvider : IClientCertificateProvider
{
    private readonly Func<ValueTask<X509Certificate>> _getCertificate;

    public TestKitClientCertificateProvider(Func<ValueTask<X509Certificate>> getCertificate)
    {
        _getCertificate = getCertificate;
    }

    public ValueTask<X509Certificate> GetCertificateAsync()
    {
        return _getCertificate();
    }
}
