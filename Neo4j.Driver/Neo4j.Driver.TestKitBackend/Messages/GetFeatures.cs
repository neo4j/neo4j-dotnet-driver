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

namespace Neo4j.Driver.TestKitBackend.Messages;

internal record GetFeaturesRequest : IProtocolMessage;

internal record FeatureListResponse : IProtocolMessage
{
    public string[] Features { get; init; } = [];
}

internal class GetFeaturesHandler : MessageHandler<GetFeaturesRequest>
{
    // Strings must match testkit's Feature enum (nutkit/protocol/feature.py); keep sorted alphabetically.
    private static readonly string[] SupportedFeatures =
    [
        "AuthorizationExpiredTreatment",
        "Backend:MockTime",
        "Backend:RTFetch",
        "Backend:RTForceUpdate",
        "ConfHint:connection.recv_timeout_seconds",
        "Detail:ClosedDriverIsEncrypted",
        "Detail:DefaultSecurityConfigValueEquality",
        "Feature:API:BookmarkManager",
        "Feature:API:ConnectionAcquisitionTimeout",
        "Feature:API:Driver.ExecuteQuery",
        "Feature:API:Driver.ExecuteQuery:WithAuth",
        "Feature:API:Driver.IsEncrypted",
        "Feature:API:Driver.SupportsSessionAuth",
        "Feature:API:Driver.VerifyAuthentication",
        "Feature:API:Driver.VerifyConnectivity",
        "Feature:API:Driver:GetServerInfo",
        "Feature:API:Driver:NotificationsConfig",
        "Feature:API:Liveness.Check",
        "Feature:API:Result.List",
        "Feature:API:Result.Peek",
        "Feature:API:Result.Single",
        "Feature:API:RetryableExceptions",
        "Feature:API:SSLClientCertificate",
        "Feature:API:SSLConfig",
        "Feature:API:SSLSchemes",
        "Feature:API:Session:AuthConfig",
        "Feature:API:Session:NotificationsConfig",
        "Feature:API:Summary:GqlStatusObjects",
        "Feature:API:Summary:Profile:OptionalStats",
        "Feature:API:Type.Spatial",
        "Feature:API:Type.Temporal",
        "Feature:API:Type.UnsupportedType",
        "Feature:API:Type.UUID",
        "Feature:API:Type.Vector",
        "Feature:Auth:Bearer",
        "Feature:Auth:Custom",
        "Feature:Auth:Kerberos",
        "Feature:Auth:Managed",
        "Feature:Bolt:3.0",
        "Feature:Bolt:4.1",
        "Feature:Bolt:4.2",
        "Feature:Bolt:4.3",
        "Feature:Bolt:4.4",
        "Feature:Bolt:5.0",
        "Feature:Bolt:5.1",
        "Feature:Bolt:5.2",
        "Feature:Bolt:5.3",
        "Feature:Bolt:5.4",
        "Feature:Bolt:5.5",
        "Feature:Bolt:5.6",
        "Feature:Bolt:5.7",
        "Feature:Bolt:5.8",
        "Feature:Bolt:6.0",
        "Feature:Bolt:6.1",
        "Feature:Bolt:HandshakeManifestV1",
        "Feature:Bolt:Patch:UTC",
        "Feature:HTTP:QueryAPI:2.0",
        "Feature:IdempotentRetries",
        "Feature:Impersonation",
        "Feature:TLS:1.2",
        "Optimization:AuthPipelining",
        "Optimization:EagerTransactionBegin",
        "Optimization:ExecuteQueryPipelining",
        "Optimization:HomeDatabaseCache",
        "Optimization:PullPipelining"
    ];

    private readonly IResponseWriter _responseWriter;
    private readonly ILogger _logger;

    public GetFeaturesHandler(IResponseWriter responseWriter, ILogger logger)
    {
        _responseWriter = responseWriter;
        _logger = logger;
    }

    public override async Task ProcessAsync(GetFeaturesRequest message)
    {
        _logger.LogDebug("Advertising {Count} feature(s)", SupportedFeatures.Length);
        await _responseWriter.WriteAsync(new FeatureListResponse { Features = SupportedFeatures });
    }
}
