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
using Neo4j.Driver.TestKitBackend.Protocol;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal record GetFeaturesRequest : IProtocolMessage;

internal record FeatureListResponse : IProtocolMessage
{
    public string[] Features { get; init; } = [];
}

internal class GetFeaturesHandler : MessageHandler<GetFeaturesRequest>
{
    // Strings must come from testkit's Feature enum (nutkit/protocol/feature.py);
    // an unknown string makes testkit raise. Keep sorted alphabetically.
    private static readonly string[] SupportedFeatures =
    [
        "Feature:API:Driver.VerifyConnectivity",
        "Feature:API:Driver:GetServerInfo"
    ];

    private readonly ILogger _logger;

    public GetFeaturesHandler(ILogger logger)
    {
        _logger = logger;
    }

    public override Task<IProtocolMessage?> ProcessAsync(GetFeaturesRequest message)
    {
        _logger.LogDebug("Advertising {Count} feature(s)", SupportedFeatures.Length);
        return Task.FromResult<IProtocolMessage?>(new FeatureListResponse { Features = SupportedFeatures });
    }
}
