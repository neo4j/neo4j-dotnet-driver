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

using Neo4j.Driver.TestKitBackend.Continuations;
using Neo4j.Driver.TestKitBackend.Dispatch;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal record ResolverResolutionRequired(string Id, string Address) : IProtocolMessage;

internal record ResolverResolutionCompletedRequest : ICallbackCompletion
{
    public required string RequestId { get; init; }
    public required string[] Addresses { get; init; }
}

internal class TestKitServerAddressResolver : IServerAddressResolver
{
    private readonly IContinuationCoordinator _coordinator;

    public TestKitServerAddressResolver(IContinuationCoordinator coordinator)
    {
        _coordinator = coordinator;
    }

    public ISet<ServerAddress> Resolve(ServerAddress address)
    {
        var pending = _coordinator.RegisterCallback();
        _coordinator.CompleteNextResponse(
            new ResolverResolutionRequired(pending.Id, $"{address.Host}:{address.Port}"));

        var completion = (ResolverResolutionCompletedRequest)pending.Completion.GetAwaiter().GetResult();
        return completion.Addresses.Select(ParseAddress).ToHashSet();
    }

    private static ServerAddress ParseAddress(string address)
    {
        var separator = address.LastIndexOf(':');
        return ServerAddress.From(address[..separator], int.Parse(address[(separator + 1)..]));
    }
}
