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
using Neo4j.Driver.TestKitBackend.Certificates;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Expectations;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal record ClientCertificateProviderRequest(string ClientCertificateProviderId) : IProtocolMessage;

internal record ClientCertificateProviderCompleted : IProtocolMessage
{
    public required string RequestId { get; init; }
    public required bool HasUpdate { get; init; }
    public required ClientCertificate ClientCertificate { get; init; }
}

internal class ClientCertificateProviderCompletedHandler : MessageHandler<ClientCertificateProviderCompleted>
{
    private readonly IExpectationStore _expectationStore;
    private readonly ICertificateLoader _certificateLoader;

    public ClientCertificateProviderCompletedHandler(
        IExpectationStore expectationStore,
        ICertificateLoader certificateLoader)
    {
        _expectationStore = expectationStore;
        _certificateLoader = certificateLoader;
    }

    public override Task ProcessAsync(ClientCertificateProviderCompleted message)
    {
        var certificate = message.ClientCertificate;
        X509Certificate loaded = _certificateLoader.Load(certificate.Certfile, certificate.Keyfile, certificate.Password);
        _expectationStore.Fulfil(message.RequestId, loaded);
        return Task.CompletedTask;
    }
}
