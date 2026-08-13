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

using Microsoft.Extensions.Logging;
using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Serialization;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal record GetServerInfoRequest : IProtocolMessage
{
    [StoredObject]
    public required IDriver Driver { get; init; }
}

internal record ServerInfoResponse(string Address, string Agent, string ProtocolVersion) : IProtocolMessage;

internal class GetServerInfoHandler : MessageHandler<GetServerInfoRequest>
{
    private readonly IResponseWriter _responseWriter;
    private readonly ILogger _logger;

    public GetServerInfoHandler(IResponseWriter responseWriter, ILogger logger)
    {
        _responseWriter = responseWriter;
        _logger = logger;
    }

    public override async Task ProcessAsync(GetServerInfoRequest message)
    {
        var info = await message.Driver.GetServerInfoAsync();
        _logger.LogDebug("Got server info");
        await _responseWriter.WriteAsync(new ServerInfoResponse(info.Address, info.Agent, info.ProtocolVersion));
    }
}
