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

using System.Globalization;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Expectations;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal record ResolverResolutionRequired(string Address) : ICorrelatedRequest
{
    public string Id { get; set; } = "";
}

internal record ResolverResolutionCompleted : IProtocolMessage
{
    public required string RequestId { get; init; }
    public required string[] Addresses { get; init; }
}

internal class ResolverResolutionCompletedHandler : MessageHandler<ResolverResolutionCompleted>
{
    private readonly IExpectationStore _expectations;

    public ResolverResolutionCompletedHandler(IExpectationStore expectations)
    {
        _expectations = expectations;
    }

    public override Task ProcessAsync(ResolverResolutionCompleted message)
    {
        _expectations.Fulfil(message.RequestId, message.Addresses);
        return Task.CompletedTask;
    }
}

internal class TestKitServerAddressResolver : IServerAddressResolver
{
    private readonly IReverseRoundTrip _roundTrip;

    public TestKitServerAddressResolver(IReverseRoundTrip roundTrip)
    {
        _roundTrip = roundTrip;
    }

    public ISet<ServerAddress> Resolve(ServerAddress address)
    {
        var resolutionRequest = new ResolverResolutionRequired($"{address.Host}:{address.Port}");
        var addresses = _roundTrip
            .SendExpectingAsync<string[]>(resolutionRequest)
            .GetAwaiter()
            .GetResult();

        return addresses.Select(ParseAddress).ToHashSet();
    }

    private static ServerAddress ParseAddress(string address)
    {
        var separator = address.LastIndexOf(':');
        var afterSeparator = address[(separator + 1)..];
        
        return int.TryParse(afterSeparator, NumberStyles.None, CultureInfo.InvariantCulture, out var port)
            ? ServerAddress.From(address[..separator], port)
            : throw new InvalidOperationException($"Invalid port number: '{afterSeparator}'");
    }
}
