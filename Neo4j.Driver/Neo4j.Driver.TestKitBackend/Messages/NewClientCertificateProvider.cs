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
using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Expectations;
using Neo4j.Driver.TestKitBackend.ObjectStorage;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal record NewClientCertificateProviderRequest : IProtocolMessage;

internal record ClientCertificateProviderResponse(string Id) : IProtocolMessage;

internal class NewClientCertificateProviderHandler : MessageHandler<NewClientCertificateProviderRequest>
{
    private readonly IObjectStore _objectStore;
    private readonly IOutboundRoundTrip _roundTrip;
    private readonly IResponseWriter _responseWriter;
    private readonly ILogger _logger;

    public NewClientCertificateProviderHandler(
        IObjectStore objectStore,
        IOutboundRoundTrip roundTrip,
        IResponseWriter responseWriter,
        ILogger logger)
    {
        _objectStore = objectStore;
        _roundTrip = roundTrip;
        _responseWriter = responseWriter;
        _logger = logger;
    }

    public override async Task ProcessAsync(NewClientCertificateProviderRequest message)
    {
        var id = _objectStore.Store(CreateStoredProvider);
        _logger.LogDebug("Created client certificate provider with id '{Id}'", id);
        await _responseWriter.WriteAsync(new ClientCertificateProviderResponse(id));
    }

    private IClientCertificateProvider CreateStoredProvider(string storageId)
    {
        ValueTask<X509Certificate> ProvideFromProvider() => ProvideCertificateAsync(storageId);
        return new TestKitClientCertificateProvider(ProvideFromProvider);
    }

    private async ValueTask<X509Certificate> ProvideCertificateAsync(string storageId)
    {
        return await _roundTrip.SendExpectingAsync<X509Certificate>(
            new ClientCertificateProviderRequest { ClientCertificateProviderId = storageId });
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
