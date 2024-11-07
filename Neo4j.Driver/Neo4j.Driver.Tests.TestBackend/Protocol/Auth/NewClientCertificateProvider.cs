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
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Neo4j.Driver.Tests.TestBackend.Protocol.Auth;

internal class NewClientCertificateProvider : ProtocolObject, IClientCertificateProvider
{
    private Controller _controller;
    public object data { get; set; } = new();
    
    public override Task Process(Controller controller)
    {
        _controller = controller;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async ValueTask<X509Certificate> GetCertificateAsync()
    {
        var requestId = Guid.NewGuid().ToString();
        var request = new ProtocolResponse(
                "ClientCertificateProviderRequest",
                new { clientCertificateProviderId = uniqueId, id = requestId })
            .Encode();
        await _controller.SendResponse(request).ConfigureAwait(false);
        var result = await _controller.TryConsumeStreamObjectOfType<ClientCertificateProviderCompleted>()
            .ConfigureAwait(false);

        if (result.data.requestId == requestId)
        {
            return ClientCertificateLoader.GetCertificate(
                result.data.clientCertificate.data.certfile,
                result.data.clientCertificate.data.keyfile,
                result.data.clientCertificate.data.password);
        }

        throw new Exception("GetCertificateAsync: request IDs did not match");
    }

    public override string Respond()
    {
        return new ProtocolResponse("ClientCertificateProvider", uniqueId).Encode();
    }
}
