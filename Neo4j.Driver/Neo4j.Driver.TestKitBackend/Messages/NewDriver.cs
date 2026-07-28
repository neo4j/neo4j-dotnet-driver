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

using Neo4j.Driver.TestKitBackend.Protocol;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal record NewDriverRequest : IProtocolMessage
{
    public string Uri { get; init; } = "";
    public AuthorizationToken? AuthorizationToken { get; init; }
}

internal record DriverResponse(string Id) : IProtocolMessage;

internal class NewDriverHandler : MessageHandler<NewDriverRequest>
{
    private readonly IRegistry _registry;

    public NewDriverHandler(IRegistry registry)
    {
        _registry = registry;
    }

    public override Task<IProtocolMessage?> ProcessAsync(NewDriverRequest message)
    {
        var driver = GraphDatabase.Driver(message.Uri, message.AuthorizationToken?.ToAuthToken());
        var registryObject = _registry.Register(driver);
        var response = new DriverResponse(registryObject.Id);
        return Task.FromResult<IProtocolMessage?>(response);
    }
}
